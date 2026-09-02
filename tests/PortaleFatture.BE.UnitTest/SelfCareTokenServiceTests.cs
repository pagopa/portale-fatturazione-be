using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Validazione del token SelfCare: firma RSA verificata contro la chiave pubblica esposta dal
/// provider (modulo/esponente in base64url). Il test genera una coppia di chiavi al volo e finge il
/// client HTTP, quindi non servono certificati reali ne' rete.
/// Copre l'altro consumatore della catena IdentityModel -> Microsoft.Bcl.Memory (l'altro e'
/// <see cref="JwtTokenServiceTests"/>): qui passano sia la decodifica base64url del token sia quella
/// di modulo ed esponente della chiave.
/// </summary>
public class SelfCareTokenServiceTests
{
    private const string Issuer = "https://selfcare.test.it";
    private const string Audience = "portale-fatturazione-test";

    private RSA _rsa = null!;
    private Mock<ISelfCareHttpClient> _httpClient = null!;
    private SelfCareTokenService _service = null!;

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

        _httpClient = new Mock<ISelfCareHttpClient>();
        _httpClient
            .Setup(x => x.GetSelfCareTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string? token, CancellationToken _) =>
                new JwtSecurityTokenHandler().ReadToken(token) as JwtSecurityToken);
        _httpClient
            .Setup(x => x.GetCertificateByKidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(certificato);

        var options = new Mock<IPortaleFattureOptions>();
        options.SetupGet(x => x.SelfCareUri).Returns(Issuer);
        options.SetupGet(x => x.SelfCareAudience).Returns(Audience);

        _service = new SelfCareTokenService(
            _httpClient.Object, options.Object, NullLogger<SelfCareTokenService>.Instance);
    }

    [TearDown]
    public void TearDown() => _rsa?.Dispose();

    [Test]
    public async Task Validate_WithTokenSignedByTheProviderKey_ShouldSucceed()
    {
        var token = CreaToken();

        (var principal, var valido) = await _service.Validate(token);

        Assert.Multiple(() =>
        {
            Assert.That(valido, Is.True);
            Assert.That(principal!.FindFirst(CustomClaim.Uid)?.Value, Is.EqualTo("uid-test"));
        });
    }

    [Test]
    public async Task ValidateContent_ShouldMap_UidEmailAndOrganization()
    {
        var token = CreaToken();

        var dto = await _service.ValidateContent(token);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Uid, Is.EqualTo("uid-test"));
            Assert.That(dto.Email, Is.EqualTo("utente@test.it"));
            Assert.That(dto.Organization, Is.Not.Null, "l'organization arriva come JSON e va deserializzata");
            Assert.That(dto.Organization!.Id, Is.EqualTo("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        });
    }

    [Test]
    public void Validate_WithTokenSignedByAnotherKey_ShouldThrow()
    {
        using var altraChiave = RSA.Create(2048);
        var token = CreaToken(chiave: altraChiave);

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token),
            "una firma prodotta con un'altra chiave non deve essere accettata");
    }

    [Test]
    public void Validate_WithWrongAudience_ShouldThrow()
    {
        var token = CreaToken(audience: "un-altro-audience");

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token),
            "l'audience e' validato");
    }

    [Test]
    public void Validate_WithWrongIssuer_ShouldThrow()
    {
        var token = CreaToken(issuer: "https://un-altro-issuer.it");

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(token),
            "l'issuer e' validato");
    }

    [Test]
    public void Validate_WithExpiredToken_ShouldThrow_OnlyWhenExpirationIsRequired()
    {
        var scaduto = CreaToken(expires: DateTime.UtcNow.AddHours(-1));

        // requireExpirationTime = false (default): la scadenza non viene controllata
        Assert.DoesNotThrowAsync(async () => await _service.Validate(scaduto),
            "col controllo di scadenza disattivato un token scaduto passa");

        Assert.ThrowsAsync<SecurityException>(async () => await _service.Validate(scaduto, requireExpirationTime: true),
            "col controllo attivo un token scaduto deve essere rifiutato");
    }

    private string CreaToken(
        RSA? chiave = null,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expires = null)
    {
        var key = new RsaSecurityKey(chiave ?? _rsa);
        var scadenza = expires ?? DateTime.UtcNow.AddHours(1);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Expires = scadenza,
            // NotBefore va indietro rispetto alla scadenza: altrimenti, per il caso "token gia'
            // scaduto", il descrittore lo valorizzerebbe a "adesso" rendendo il token incoerente
            // (IDX12401) e il test fallirebbe in costruzione invece che in validazione.
            NotBefore = scadenza.AddHours(-2),
            IssuedAt = scadenza.AddHours(-2),
            Subject = new ClaimsIdentity(
            [
                new Claim(CustomClaim.Uid, "uid-test"),
                new Claim(ClaimTypes.Email, "utente@test.it"),
                new Claim(CustomClaim.Organization,
                    """{"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","name":"Ente di test"}""",
                    Microsoft.IdentityModel.JsonWebTokens.JsonClaimValueTypes.Json)
            ]),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
