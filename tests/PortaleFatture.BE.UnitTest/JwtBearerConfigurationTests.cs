using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Identity;

namespace PortaleFatture.BE.UnitTest;

/// <summary>
/// Configurazione dello schema JwtBearer (ConfigurationExtensions.JwtAuthenticationConfiguration),
/// cioe' le regole con cui l'API accetta o rifiuta il JWT interno emesso dopo lo scambio SelfCare.
///
/// Perche' serve un test dedicato: e' l'unico punto del prodotto in cui la validazione del token e'
/// delegata al middleware ASP.NET invece che a codice nostro, e l'harness HTTP degli integration
/// test NON lo esercita — ApiTestFactory sostituisce lo schema con TestAuthHandler proprio per
/// poter testare routing e [Authorize] senza token veri. Senza questi casi, una regressione della
/// pipeline JwtBearer (tipicamente un cambio di versione dello stack Microsoft.IdentityModel.*)
/// passerebbe l'intera suite e si vedrebbe solo in ambiente.
///
/// I test sono COMPORTAMENTALI, non specchio delle assegnazioni: si valida un token vero contro i
/// TokenValidationParameters prodotti dalla configurazione. Per lo stesso motivo i rifiuti usano
/// Assert.Catch (che accetta anche i tipi derivati) sulle categorie storicamente stabili di
/// IdentityModel — Invalid{Issuer,Audience}, Expired, NoExpiration — e non Assert.Throws, che in
/// NUnit pretende il tipo ESATTO e legherebbe i test alla versione dello stack: devono restare
/// validi anche se lo stack venisse alzato o abbassato.
/// </summary>
public class JwtBearerConfigurationTests
{
    // HmacSha256 vuole una chiave di almeno 256 bit: sotto i 32 caratteri IdentityModel rifiuta.
    private const string Secret = "chiave-di-test-lunga-almeno-32-caratteri!";
    private const string AltroSecret = "altra-chiave-di-test-lunga-almeno-32-car!";
    private const string Issuer = "portale-fatturazione-test";
    private const string Audience = "portale-fatturazione-client";
    private const string IdEnteTest = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    private static JwtConfiguration Configurazione() =>
        new() { ValidIssuer = Issuer, ValidAudience = Audience, Secret = Secret };

    private static JwtBearerOptions Opzioni() =>
        new JwtBearerOptions().JwtAuthenticationConfiguration(Configurazione());

    private static ClaimsPrincipal Valida(string token) =>
        new JwtSecurityTokenHandler().ValidateToken(token, Opzioni().TokenValidationParameters, out _);

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

    private static string Token(
        string issuer = Issuer,
        string audience = Audience,
        string secret = Secret,
        DateTime? scadenza = null,
        bool senzaScadenza = false)
    {
        var credenziali = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer,
            audience,
            Claims(),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: senzaScadenza ? null : scadenza ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: credenziali);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static AuthenticationFailedContext ContestoDiFallimento(JwtBearerOptions opzioni, Exception eccezione) =>
        new(new DefaultHttpContext(),
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            opzioni)
        { Exception = eccezione };

    /// <summary>
    /// Il caso che conta: il token che l'API emette deve essere accettato dallo schema con cui la
    /// stessa API lo rivalida. Emissione (JwtTokenService) e validazione (JwtBearer) sono due punti
    /// di codice distinti che condividono solo la JwtConfiguration.
    /// </summary>
    [Test]
    public void JwtAuthenticationConfiguration_ShouldAccept_TokenIssuedByJwtTokenService()
    {
        var opzioni = Opzioni();
        var profilo = new JwtTokenService(Configurazione()).GenerateJwtToken(Claims());

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(profilo.JWT!, opzioni.TokenValidationParameters, out var token);

        Assert.Multiple(() =>
        {
            Assert.That(opzioni.SaveToken, Is.True, "SaveToken serve a rileggere il token dall'HttpContext.");
            Assert.That(principal.FindFirst(CustomClaim.IdEnte)?.Value, Is.EqualTo(IdEnteTest));
            Assert.That(principal.FindFirst(ClaimTypes.Role)?.Value, Is.EqualTo(Ruolo.ADMIN));
            Assert.That(principal.FindFirst(CustomClaim.Auth)?.Value, Is.EqualTo("SELFCARE"));
            Assert.That(((JwtSecurityToken)token).Header.Alg, Is.EqualTo(SecurityAlgorithms.HmacSha256));
        });
    }

    /// <summary>
    /// Firma non verificabile: a seconda della versione di IdentityModel l'eccezione e'
    /// SecurityTokenInvalidSignatureException oppure SecurityTokenSignatureKeyNotFoundException
    /// (IDX10517, "kid" assente), e le due NON sono in relazione di ereditarieta'. Si accettano
    /// entrambe: quello che deve restare vero e' il rifiuto per motivi di firma.
    /// </summary>
    [Test]
    public void JwtAuthenticationConfiguration_ShouldReject_TokenSignedWithAnotherSecret()
    {
        var eccezione = Assert.Catch<SecurityTokenValidationException>(() => Valida(Token(secret: AltroSecret)));

        Assert.That(eccezione, Is.InstanceOf<SecurityTokenInvalidSignatureException>()
                                 .Or.InstanceOf<SecurityTokenSignatureKeyNotFoundException>());
    }

    [Test]
    public void JwtAuthenticationConfiguration_ShouldReject_TokenWithWrongIssuer() =>
        Assert.Catch<SecurityTokenInvalidIssuerException>(() => Valida(Token(issuer: "issuer-non-nostro")));

    [Test]
    public void JwtAuthenticationConfiguration_ShouldReject_TokenWithWrongAudience() =>
        Assert.Catch<SecurityTokenInvalidAudienceException>(() => Valida(Token(audience: "altra-audience")));

    /// <summary>
    /// ClockSkew e' impostata a zero, contro il default di 5 minuti di IdentityModel: un token
    /// scaduto da un solo minuto deve gia' essere rifiutato. Se qualcuno togliesse quella riga il
    /// token resterebbe valido per altri 5 minuti e nessun altro test se ne accorgerebbe.
    /// </summary>
    [Test]
    public void JwtAuthenticationConfiguration_ShouldReject_TokenExpiredByOneMinute_PerClockSkewZero() =>
        Assert.Catch<SecurityTokenExpiredException>(
            () => Valida(Token(scadenza: DateTime.UtcNow.AddMinutes(-1))));

    /// <summary>
    /// RequireExpirationTime: un token senza claim 'exp' non e' accettabile, altrimenti sarebbe
    /// eterno (ValidateLifetime da solo non basta, senza exp non avrebbe nulla da confrontare).
    /// </summary>
    [Test]
    public void JwtAuthenticationConfiguration_ShouldReject_TokenWithoutExpiration() =>
        Assert.Catch<SecurityTokenNoExpirationException>(() => Valida(Token(senzaScadenza: true)));

    /// <summary>
    /// L'header IS-TOKEN-EXPIRED e' il segnale con cui il frontend distingue "sessione scaduta"
    /// (rifare login) da un 401 qualunque: e' contratto verso il client, non un dettaglio interno.
    /// </summary>
    [Test]
    public async Task OnAuthenticationFailed_ConTokenScaduto_ShouldSetIsTokenExpiredHeader()
    {
        var opzioni = Opzioni();
        var contesto = ContestoDiFallimento(opzioni, new SecurityTokenExpiredException("scaduto"));

        await opzioni.Events!.OnAuthenticationFailed(contesto);

        Assert.That(contesto.Response.Headers["IS-TOKEN-EXPIRED"].ToString(), Is.EqualTo("true"));
    }

    [Test]
    public async Task OnAuthenticationFailed_ConAltroErrore_ShouldNotSetIsTokenExpiredHeader()
    {
        var opzioni = Opzioni();
        var contesto = ContestoDiFallimento(opzioni, new SecurityTokenInvalidSignatureException("firma"));

        await opzioni.Events!.OnAuthenticationFailed(contesto);

        Assert.That(contesto.Response.Headers.ContainsKey("IS-TOKEN-EXPIRED"), Is.False);
    }

    [TestCase(null, Audience, Secret, TestName = "Senza ValidIssuer")]
    [TestCase(Issuer, null, Secret, TestName = "Senza ValidAudience")]
    [TestCase(Issuer, Audience, null, TestName = "Senza Secret")]
    public void JwtAuthenticationConfiguration_ConConfigurazioneIncompleta_ShouldThrow(
        string? issuer, string? audience, string? secret)
    {
        var configurazione = new JwtConfiguration { ValidIssuer = issuer, ValidAudience = audience, Secret = secret };

        Assert.Throws<ConfigurationException>(
            () => new JwtBearerOptions().JwtAuthenticationConfiguration(configurazione));
    }

    [Test]
    public void JwtAuthenticationConfiguration_SenzaConfigurazione_ShouldThrow() =>
        Assert.Throws<ConfigurationException>(
            () => new JwtBearerOptions().JwtAuthenticationConfiguration(null!));
}
