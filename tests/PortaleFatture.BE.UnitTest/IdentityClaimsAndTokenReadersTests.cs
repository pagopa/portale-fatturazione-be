using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Ultimi due consumatori della catena IdentityModel: la costruzione dei claim
/// (<see cref="IdentityUsersService"/>, che usa JwtRegisteredClaimNames) e la lettura del token nei
/// due client HTTP (<c>GetSelfCareTokenAsync</c>, che fa ReadToken e quindi decodifica base64url).
/// Il test di catena completa claim -> JWT -> ProfileInfo e' il piu' utile dei tre: intercetta un
/// claim mancante, che altrimenti si manifesterebbe a runtime come NullReferenceException nel Mapper.
/// </summary>
public class IdentityClaimsAndTokenReadersTests
{
    private const string Secret = "chiave-di-test-lunga-almeno-32-caratteri!";

    private static AuthenticationInfo AuthSelfCare() => new()
    {
        Id = "utente-test",
        Ruolo = Ruolo.ADMIN,
        DescrizioneRuolo = "Amministratore",
        Profilo = "PA",
        Prodotto = "prod-pn",
        IdEnte = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        NomeEnte = "Ente di test",
        GruppoRuolo = "gruppo-test",
        Auth = "SELFCARE",
        Email = "utente@test.it",
        IdTipoContratto = 2
    };

    [Test]
    public void GetUserClaimsFromSelfCareUser_ShouldProduce_AllClaimsRequiredByTheProfile()
    {
        var claims = new IdentityUsersService().GetUserClaimsFromSelfCareUserAsync(AuthSelfCare());

        Assert.Multiple(() =>
        {
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Name && c.Value == "utente-test"), Is.True);
            Assert.That(claims.Any(c => c.Type == ClaimTypes.Role && c.Value == Ruolo.ADMIN), Is.True);
            Assert.That(claims.Any(c => c.Type == CustomClaim.IdEnte), Is.True);
            Assert.That(claims.Any(c => c.Type == CustomClaim.NomeEnte), Is.True);
            Assert.That(claims.Any(c => c.Type == CustomClaim.GruppoRuolo), Is.True);
            Assert.That(claims.Any(c => c.Type == CustomClaim.Auth), Is.True);
            Assert.That(claims.Any(c => c.Type == CustomClaim.IdTipoContratto && c.Value == "2"), Is.True,
                "IdTipoContratto e' opzionale ma se valorizzato deve finire nei claim");
            Assert.That(claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti), Is.True,
                "il jti rende univoco il token");
        });
    }

    [Test]
    public void GetUserClaimsFromSelfCareUser_WithMissingMandatoryData_ShouldThrow()
    {
        var incompleto = AuthSelfCare();
        incompleto.GruppoRuolo = null;

        Assert.Throws<SecurityException>(
            () => new IdentityUsersService().GetUserClaimsFromSelfCareUserAsync(incompleto),
            "un dato obbligatorio mancante deve fermare l'emissione, non produrre un token monco");
    }

    [Test]
    public void GetUserClaimsFromPagoPAUser_ShouldLeave_EnteClaimsEmpty()
    {
        // Un operatore pagoPA non e' legato a un ente: i due claim esistono ma sono vuoti.
        var claims = new IdentityUsersService().GetUserClaimsFromPagoPAUserAsync(AuthSelfCare(), "prod-pn");

        Assert.Multiple(() =>
        {
            Assert.That(claims.First(c => c.Type == CustomClaim.IdEnte).Value, Is.Empty);
            Assert.That(claims.First(c => c.Type == CustomClaim.NomeEnte).Value, Is.Empty);
            Assert.That(claims.First(c => c.Type == CustomClaim.Prodotto).Value, Is.EqualTo("prod-pn"),
                "il prodotto arriva dal parametro, non dall'AuthenticationInfo");
        });
    }

    [Test]
    public void ClaimsFromSelfCare_ShouldBeEnough_ToIssueAndReadBackTheInternalJwt()
    {
        // Catena completa: claim -> JWT -> ProfileInfo -> ri-lettura del token.
        var claims = new IdentityUsersService().GetUserClaimsFromSelfCareUserAsync(AuthSelfCare());
        var tokenService = new JwtTokenService("test-audience", "test-issuer", Secret);

        var profilo = tokenService.GenerateJwtToken(claims);
        var principal = tokenService.GetPrincipalFromExpiredToken(profilo.JWT!);

        Assert.Multiple(() =>
        {
            Assert.That(profilo.IdEnte, Is.EqualTo("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
            Assert.That(profilo.IdTipoContratto, Is.EqualTo(2));
            Assert.That(principal.FindFirst(CustomClaim.NomeEnte)?.Value, Is.EqualTo("Ente di test"));
        });
    }

    [Test]
    public void GetSelfCareTokenAsync_ShouldReadHeaderAndClaims_WithoutValidating()
    {
        // I due client si limitano a leggere il token per estrarne il kid: nessuna verifica di firma.
        var jwt = new JwtTokenService("aud", "iss", Secret)
            .GenerateJwtToken(new IdentityUsersService().GetUserClaimsFromSelfCareUserAsync(AuthSelfCare())).JWT!;

        var selfCare = new SelfCareHttpClient(
            new Mock<IPortaleFattureOptions>().Object,
            new Mock<IHttpClientFactory>().Object,
            NullLogger<SelfCareHttpClient>.Instance);

        var pagoPA = new PagoPAHttpClient(
            new Mock<IPortaleFattureOptions>().Object,
            new Mock<IHttpClientFactory>().Object,
            NullLogger<PagoPAHttpClient>.Instance);

        var lettoSelfCare = selfCare.GetSelfCareTokenAsync(jwt);
        var lettoPagoPA = pagoPA.GetSelfCareTokenAsync(jwt);

        Assert.Multiple(() =>
        {
            Assert.That(lettoSelfCare, Is.Not.Null);
            Assert.That(lettoSelfCare!.Claims.Any(c => c.Type == CustomClaim.IdEnte), Is.True);
            Assert.That(lettoPagoPA, Is.Not.Null);
            Assert.That(lettoPagoPA!.Header.Alg, Is.EqualTo(lettoSelfCare.Header.Alg),
                "i due client leggono lo stesso token allo stesso modo");
        });
    }

    [Test]
    public void GetSelfCareTokenAsync_WithGarbage_ShouldThrow()
    {
        var selfCare = new SelfCareHttpClient(
            new Mock<IPortaleFattureOptions>().Object,
            new Mock<IHttpClientFactory>().Object,
            NullLogger<SelfCareHttpClient>.Instance);

        Assert.Catch<ArgumentException>(() => selfCare.GetSelfCareTokenAsync("non-e-un-token"),
            "una stringa non JWT non deve essere letta in silenzio");
    }
}
