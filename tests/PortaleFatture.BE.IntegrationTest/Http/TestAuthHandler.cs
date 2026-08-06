using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Schema di autenticazione fittizio per i test HTTP: costruisce una identity con i claim che
/// l'app si aspetta (GetAuthInfo().Mapper()) e che soddisfano le policy (claim "auth").
///
/// Il ruolo si passa per header, cosi' una sola factory serve tutti gli scenari:
///   - nessun header  -> richiesta ANONIMA (per verificare 401)
///   - X-Test-Ruolo: ADMIN|OPERATOR ...
///   - X-Test-Auth:  PAGOPA (default) | SELFCARE
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "TestScheme";
    public const string RoleHeader = "X-Test-Ruolo";
    public const string AuthHeader = "X-Test-Auth";
    public const string IdEnteHeader = "X-Test-IdEnte";

    /// <summary>
    /// SelfCarePolicy pretende, oltre a auth = SELFCARE, un `profilo` fra quelli ammessi (PA, GSP,
    /// SCP, PSP, AS, SA, PT). Il valore fisso "profilo-test" li fallisce tutti, quindi senza questo
    /// header le rotte lato aderente rispondono 403 anche con l'identita' giusta.
    /// </summary>
    public const string ProfiloHeader = "X-Test-Profilo";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var ruoloHeader))
            return Task.FromResult(AuthenticateResult.NoResult()); // anonimo -> 401 dalle rotte protette

        var ruolo = ruoloHeader.ToString();
        var auth = Request.Headers.TryGetValue(AuthHeader, out var a) && !string.IsNullOrWhiteSpace(a)
            ? a.ToString()
            : AuthType.PAGOPA;
        var idEnte = Request.Headers.TryGetValue(IdEnteHeader, out var e) && !string.IsNullOrWhiteSpace(e)
            ? e.ToString()
            : "11111111-1111-1111-1111-111111111111";
        var profilo = Request.Headers.TryGetValue(ProfiloHeader, out var p) && !string.IsNullOrWhiteSpace(p)
            ? p.ToString()
            : "profilo-test";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "integration-test-user"),
            new(ClaimTypes.Role, ruolo),
            new(ClaimTypes.Email, "test@pagopa.it"),
            new(CustomClaim.Auth, auth),
            new(CustomClaim.DescrizioneRuolo, ruolo),
            new(CustomClaim.Profilo, profilo),
            new(CustomClaim.Prodotto, "prod-pn"),
            new(CustomClaim.IdEnte, idEnte),
            new(CustomClaim.NomeEnte, "Ente Test"),
            new(CustomClaim.GruppoRuolo, "gruppo-test"),
            new(CustomClaim.IdTipoContratto, "1"),
        };

        var identity = new ClaimsIdentity(claims, Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
