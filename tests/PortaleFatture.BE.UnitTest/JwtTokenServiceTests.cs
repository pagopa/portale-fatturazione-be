using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Identity;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Emissione e ri-validazione del JWT interno (quello emesso dopo lo scambio con SelfCare).
/// Oltre a coprire una classe che non aveva test, e' il test funzionale della catena
/// System.IdentityModel.Tokens.Jwt -> Microsoft.Bcl.Memory.
/// NB: il progetto ha come target net8.0. Il tipo System.Buffers.Text.Base64Url, usato da
/// IdentityModel per (de)serializzare i token, e' entrato nel runtime .NET solo dalla 9: su net8
/// lo fornisce il pacchetto Microsoft.Bcl.Memory come backport (la sua dll per net8.0 pesa ~58 KB,
/// quella per net9.0 ~16 KB perche' e' solo type-forward). Il codice di quel pacchetto viene quindi
/// eseguito davvero qui, ed e' il motivo per cui una regressione del suo aggiornamento si vedrebbe
/// in questi test.
/// </summary>
public class JwtTokenServiceTests
{
    // HmacSha256 vuole una chiave di almeno 256 bit: sotto i 32 caratteri IdentityModel rifiuta.
    private const string Secret = "chiave-di-test-lunga-almeno-32-caratteri!";
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const string IdEnteTest = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    private static JwtTokenService Service(string secret = Secret) => new(Audience, Issuer, secret);

    private static IList<Claim> Claims() =>
    [
        new(ClaimTypes.Name, "utente-test"),
        new(ClaimTypes.Role, Ruolo.ADMIN),
        new(ClaimTypes.Email, "utente@test.it"),
        new(CustomClaim.DescrizioneRuolo, "Amministratore"),
        new(CustomClaim.IdEnte, IdEnteTest),
        new(CustomClaim.Prodotto, "prod-pn"),
        new(CustomClaim.Profilo, "PA"),
        new(CustomClaim.GruppoRuolo, "gruppo-test"),
        new(CustomClaim.Auth, "SELFCARE")
    ];

    [Test]
    public void GenerateJwtToken_ThenValidate_ShouldRoundTripClaims()
    {
        var service = Service();

        var profilo = service.GenerateJwtToken(Claims());
        var principal = service.GetPrincipalFromExpiredToken(profilo.JWT!);

        Assert.Multiple(() =>
        {
            Assert.That(principal.FindFirst(CustomClaim.IdEnte)?.Value, Is.EqualTo(IdEnteTest));
            Assert.That(principal.FindFirst(ClaimTypes.Role)?.Value, Is.EqualTo(Ruolo.ADMIN));
            Assert.That(principal.FindFirst(CustomClaim.Auth)?.Value, Is.EqualTo("SELFCARE"));
            Assert.That(principal.FindFirst(CustomClaim.Prodotto)?.Value, Is.EqualTo("prod-pn"));
        });
    }

    [Test]
    public void GenerateJwtToken_ShouldMapProfileInfo_FromClaims()
    {
        var profilo = Service().GenerateJwtToken(Claims());

        Assert.Multiple(() =>
        {
            Assert.That(profilo.IdEnte, Is.EqualTo(IdEnteTest));
            Assert.That(profilo.Ruolo, Is.EqualTo(Ruolo.ADMIN));
            Assert.That(profilo.DescrizioneRuolo, Is.EqualTo("Amministratore"));
            Assert.That(profilo.Auth, Is.EqualTo("SELFCARE"));
            Assert.That(profilo.Email, Is.EqualTo("utente@test.it"));
            Assert.That(profilo.JWT, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void GenerateJwtToken_ShouldProduce_ThreeBase64UrlSegments()
    {
        // Asserzione mirata sulla codifica prodotta dallo stack IdentityModel: tre segmenti e
        // alfabeto base64url (niente +, /, =). E' il punto in cui interviene Microsoft.Bcl.Memory.
        var jwt = Service().GenerateJwtToken(Claims()).JWT!;

        var segmenti = jwt.Split('.');

        Assert.Multiple(() =>
        {
            Assert.That(segmenti, Has.Length.EqualTo(3), "un JWT compatto e' header.payload.firma");
            Assert.That(segmenti.All(s => s.Length > 0), Is.True, "nessun segmento vuoto");
            Assert.That(jwt, Does.Not.Contain("+").And.Not.Contain("/").And.Not.Contain("="),
                "la codifica deve essere base64url, non base64 standard");
            Assert.That(new JwtSecurityTokenHandler().CanReadToken(jwt), Is.True,
                "il token deve essere leggibile da un handler indipendente");
        });
    }

    [Test]
    public void GenerateJwtToken_ShouldSetIssuerAudienceAndHmacSha256()
    {
        var jwt = Service().GenerateJwtToken(Claims()).JWT!;

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        Assert.Multiple(() =>
        {
            Assert.That(token.Issuer, Is.EqualTo(Issuer));
            Assert.That(token.Audiences, Does.Contain(Audience));
            Assert.That(token.Header.Alg, Is.EqualTo(SecurityAlgorithms.HmacSha256));
        });
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithDifferentKey_ShouldThrow()
    {
        var jwt = Service().GenerateJwtToken(Claims()).JWT!;
        var altroServizio = Service("un-altra-chiave-lunga-almeno-32-caratteri!");

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(
            () => altroServizio.GetPrincipalFromExpiredToken(jwt),
            "una chiave diversa non deve validare: la firma va verificata");
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithTamperedPayload_ShouldThrow()
    {
        // Payload di un secondo token (base64url valido, altri claim) unito alla firma del primo:
        // cosi' il fallimento e' della VERIFICA DELLA FIRMA, non della decodifica.
        var service = Service();

        var originale = service.GenerateJwtToken(Claims()).JWT!.Split('.');
        var altriClaim = Claims();
        altriClaim[4] = new Claim(CustomClaim.IdEnte, "ffffffff-ffff-ffff-ffff-ffffffffffff");
        var altro = service.GenerateJwtToken(altriClaim).JWT!.Split('.');

        var manomesso = $"{altro[0]}.{altro[1]}.{originale[2]}";

        // Con chiave simmetrica (nessun kid nell'header) IdentityModel segnala il fallimento come
        // SecurityTokenSignatureKeyNotFoundException: si asserisce il tipo base + il messaggio, per
        // non legare il test a una sottoclasse che puo' cambiare tra versioni della libreria.
        var ex = Assert.Catch<SecurityTokenException>(
            () => service.GetPrincipalFromExpiredToken(manomesso),
            "un payload sostituito non deve superare la verifica della firma");

        Assert.That(ex!.Message, Does.Contain("Signature validation failed"));
    }

    [Test]
    public void GetPrincipalFromExpiredToken_WithMalformedBase64UrlPayload_ShouldThrow()
    {
        // Token con forma valida (tre segmenti) ma payload non decodificabile: il rifiuto arriva dal
        // decoder base64url (System.Buffers.Text.Base64Url, fornito su net8 da Microsoft.Bcl.Memory),
        // prima ancora della verifica della firma.
        var service = Service();
        var parti = service.GenerateJwtToken(Claims()).JWT!.Split('.');
        var malformato = $"{parti[0]}.{parti[1][..^2]}XX.{parti[2]}";

        Assert.Throws<ArgumentException>(
            () => service.GetPrincipalFromExpiredToken(malformato),
            "un payload non decodificabile come base64url deve essere rifiutato");
    }

    [Test]
    public void GetPrincipalFromExpiredToken_ShouldAccept_AnExpiredToken()
    {
        // Il metodo serve proprio a rileggere un token scaduto (ValidateLifetime = false):
        // la scadenza non deve essere un motivo di rifiuto, la firma si.
        var service = Service();
        var jwt = service.GenerateJwtToken(Claims()).JWT!;

        Assert.DoesNotThrow(() => service.GetPrincipalFromExpiredToken(jwt));
    }
}
