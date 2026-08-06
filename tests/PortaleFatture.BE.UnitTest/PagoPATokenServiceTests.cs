using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Validazione del token Azure AD del pannello amministratore: stessa meccanica RSA di
/// <see cref="SelfCareTokenServiceTests"/> (firma verificata contro modulo/esponente in base64url),
/// ma issuer/audience presi dalla configurazione AzureAd e arricchimento dei gruppi via Graph.
/// Terzo consumatore della catena IdentityModel -> Microsoft.Bcl.Memory.
/// </summary>
public class PagoPATokenServiceTests
{
    private const string TenantId = "11111111-2222-3333-4444-555555555555";
    private const string ClientId = "99999999-8888-7777-6666-555555555555";
    private const string IdGruppo = "gruppo-fatturazione-admin";
    private static string Issuer => $"https://login.microsoftonline.com/{TenantId}/v2.0";

    private RSA _rsa = null!;
    private Mock<IPagoPAHttpClient> _httpClient = null!;
    private Mock<IMicrosoftGraphHttpClient> _graph = null!;
    private PagoPATokenService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _rsa = RSA.Create(2048);

        var parametri = _rsa.ExportParameters(false);
        var certificato = new CertificateKey
        {
            Kid = "test-kid",
            Alg = "RS256",
            Kty = "RSA",
            Use = "sig",
            N = Base64Url(parametri.Modulus!),
            E = Base64Url(parametri.Exponent!)
        };

        _httpClient = new Mock<IPagoPAHttpClient>();
        _httpClient
            .Setup(x => x.GetSelfCareTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string? token, CancellationToken _) =>
                new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken);
        _httpClient
            .Setup(x => x.GetCertificateByKidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificato);

        _graph = new Mock<IMicrosoftGraphHttpClient>();
        _graph
            .Setup(x => x.GetGroupsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string?>
            {
                [IdGruppo] = "Portale Fatturazione - Amministratori",
                ["gruppo-non-pertinente"] = "Altro gruppo"
            });

        var options = new Mock<IPortaleFattureOptions>();
        options.SetupGet(x => x.AzureAd).Returns(new AzureAd { TenantId = TenantId, ClientId = ClientId });

        _service = new PagoPATokenService(
            _httpClient.Object, _graph.Object, options.Object, NullLogger<PagoPATokenService>.Instance);
    }

    [TearDown]
    public void TearDown() => _rsa?.Dispose();

    [Test]
    public async Task Validate_WithTokenSignedByTheTenantKey_ShouldSucceed()
    {
        var token = CreaToken();

        (var principal, var valido) = await _service.Validate(token);

        Assert.Multiple(() =>
        {
            Assert.That(valido, Is.True);
            Assert.That(principal!.FindFirst(CustomClaim.PreferredUsername)?.Value,
                Is.EqualTo("operatore@pagopa.it"));
        });
    }

    [Test]
    public async Task ValidateContent_ShouldMapProfile_AndKeepOnlyTheGroupsOfTheUserRoles()
    {
        var token = CreaToken();

        var dto = await _service.ValidateContent(token, "access-token-graph");

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Email, Is.EqualTo("operatore@pagopa.it"));
            Assert.That(dto.Uid, Is.EqualTo("oid-test"));
            Assert.That(dto.Name, Is.EqualTo("Operatore Test"));
            Assert.That(dto.Roles, Is.EquivalentTo(new[] { IdGruppo }));
            Assert.That(dto.Groups, Is.EquivalentTo(new[] { "Portale Fatturazione - Amministratori" }),
                "dei gruppi restituiti da Graph vanno tenuti solo quelli corrispondenti ai ruoli del token");
        });
    }

    [Test]
    public void Validate_WithTokenSignedByAnotherKey_ShouldThrow()
    {
        using var altraChiave = RSA.Create(2048);
        var token = CreaToken(chiave: altraChiave);

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token));
    }

    [Test]
    public void Validate_WithWrongAudience_ShouldThrow()
    {
        var token = CreaToken(audience: "un-altro-client-id");

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token),
            "l'audience deve corrispondere al ClientId configurato");
    }

    [Test]
    public void Validate_WithWrongTenant_ShouldThrow()
    {
        var token = CreaToken(issuer: "https://login.microsoftonline.com/un-altro-tenant/v2.0");

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token),
            "l'issuer include il tenant: un tenant diverso non deve validare");
    }

    [Test]
    public void ValidateContent_WithoutRoleClaims_ShouldThrowRoleException()
    {
        var token = CreaToken(conRuoli: false);

        Assert.ThrowsAsync<RoleException>(
            async () => await _service.ValidateContent(token, "access-token-graph"),
            "senza ruoli nel token non si puo' risolvere l'appartenenza ai gruppi");
    }

    private string CreaToken(
        RSA? chiave = null,
        string? issuer = null,
        string audience = ClientId,
        bool conRuoli = true)
    {
        var claims = new List<Claim>
        {
            new(CustomClaim.PreferredUsername, "operatore@pagopa.it"),
            new(CustomClaim.Oid, "oid-test"),
            new(CustomClaim.Name, "Operatore Test")
        };
        if (conRuoli)
            claims.Add(new Claim(ClaimTypes.Role, IdGruppo));

        var scadenza = DateTime.UtcNow.AddHours(1);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = audience,
            Expires = scadenza,
            NotBefore = scadenza.AddHours(-2),
            IssuedAt = scadenza.AddHours(-2),
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(chiave ?? _rsa), SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
