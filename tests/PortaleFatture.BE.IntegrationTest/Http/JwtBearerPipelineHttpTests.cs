using System.Net;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP end-to-end della pipeline JwtBearer REALE (JwtApiTestFactory, non TestAuthHandler):
/// verifica il wiring, cioe' che la configurazione prodotta da JwtAuthenticationConfiguration sia
/// davvero quella con cui l'API accetta o rifiuta un Bearer token in ingresso. Complementa
/// JwtBearerConfigurationTests nel progetto unit, che copre le regole di validazione in isolamento:
/// qui si prova che quelle regole siano effettivamente montate sullo schema di default.
///
/// Bersaglio: GET api/auth/profilo — [Authorize(Roles = OPERATOR, ADMIN)] e in whitelist del
/// NonceMultiTabsMiddleware, quindi la richiesta arriva all'autorizzazione senza bisogno del nonce.
///
/// I casi di rifiuto NON toccano il database: authentication e authorization decidono prima che
/// l'handler venga invocato, quindi girano anche a container spento. Solo l'ultimo caso (token
/// valido e ruolo autorizzato) arriva all'handler, e infatti asserisce soltanto che la richiesta
/// abbia superato auth — non l'esito, che dipende dai dati.
/// </summary>
public class JwtBearerPipelineHttpTests
{
    private JwtApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new JwtApiTestFactory();

    [SetUp]
    public void CheckConfigurazione() => _factory.SkipSeConfigurazioneJwtAssente();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Get(string? token)
    {
        var resp = await _factory.CreateClientWithToken(token).GetAsync(JwtApiTestFactory.RottaProtetta);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        // Diagnostico: JwtBearer motiva il rifiuto qui (error="invalid_token", error_description=IDX...).
        // Se l'header manca su un 401, a rispondere non e' stato JwtBearer ma un altro schema.
        TestContext.Out.WriteLine($"WWW-Authenticate: {string.Join(" | ", resp.Headers.WwwAuthenticate)}");
        if ((int)resp.StatusCode >= 500)
            TestContext.Out.WriteLine($"BODY: {await resp.Content.ReadAsStringAsync()}");
        return resp;
    }

    [Test]
    public async Task SenzaToken_ShouldReturn401()
    {
        var resp = await Get(null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TokenFirmatoConAltroSecret_ShouldReturn401()
    {
        var altroSecret = JwtApiTestFactory.SecretDiverso(_factory.ConfigurazioneJwt!.Secret!);

        var resp = await Get(_factory.Token(secret: altroSecret));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TokenConIssuerErrato_ShouldReturn401()
    {
        var resp = await Get(_factory.Token(issuer: "issuer-non-nostro"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TokenConAudienceErrata_ShouldReturn401()
    {
        var resp = await Get(_factory.Token(audience: "altra-audience"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// Doppia verifica in un colpo solo: ClockSkew a zero (un minuto di ritardo basta a rifiutare,
    /// col default di 5 minuti passerebbe) e l'evento OnAuthenticationFailed montato davvero sulla
    /// pipeline — l'header IS-TOKEN-EXPIRED e' il segnale con cui il frontend distingue la sessione
    /// scaduta da un 401 qualsiasi.
    /// </summary>
    [Test]
    public async Task TokenScaduto_ShouldReturn401_ConHeaderIsTokenExpired()
    {
        var resp = await Get(_factory.Token(scadenza: DateTime.UtcNow.AddMinutes(-1)));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(resp.Headers.TryGetValues("IS-TOKEN-EXPIRED", out var valori) && valori.Contains("true"),
                Is.True, "L'header IS-TOKEN-EXPIRED e' contratto verso il frontend, non un dettaglio interno.");
        });
    }

    /// <summary>
    /// 403 e non 401: il token e' stato ACCETTATO (firma, issuer, audience, scadenza) e a fermare la
    /// richiesta e' il ruolo. E' la prova positiva dell'autenticazione senza toccare il database.
    /// </summary>
    [Test]
    public async Task TokenValidoConRuoloNonAutorizzato_ShouldReturn403()
    {
        var resp = await Get(_factory.Token(ruolo: "RUOLO-INESISTENTE"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Token valido con ruolo ammesso: si asserisce solo che auth e authorization siano superate
    /// (ne' 401 ne' 403), non l'esito della rotta. Oggi infatti l'handler risponde 500 perche' il DB
    /// seedato non ha la tabella pfw.Utenti che UtenteCreateCommand interroga ("Invalid object name
    /// 'pfw.utenti'"): e' un buco del seed, estraneo alla pipeline di autenticazione. Se un domani
    /// la tabella venisse aggiunta al seed, questo test resterebbe valido e si potrebbe irrigidire
    /// l'asserzione a 200.
    /// </summary>
    [Test]
    public async Task TokenValidoConRuoloAdmin_ShouldPassAuthenticationAndAuthorization()
    {
        var resp = await Get(_factory.Token(ruolo: Ruolo.ADMIN));

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized)
                                       .And.Not.EqualTo(HttpStatusCode.Forbidden));
    }
}
