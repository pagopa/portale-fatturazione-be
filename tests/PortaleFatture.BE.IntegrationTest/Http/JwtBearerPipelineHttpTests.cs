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
    /// Token valido con ruolo ammesso: la richiesta arriva fino in fondo e la rotta risponde 200 con
    /// il profilo. Fino al 06/08/2026 questo test si limitava a "ne' 401 ne' 403" perche' l'handler
    /// finiva in 500: mancava dal seed la tabella pfw.Utenti, che UtenteCreateCommand interroga a ogni
    /// chiamata. Aggiunta al seed, l'asserzione e' stata irrigidita — e' il caso BE-AUTH-04 del testbook.
    /// </summary>
    [Test]
    public async Task TokenValidoConRuoloAdmin_ShouldReturn200_ConProfilo()
    {
        var resp = await Get(_factory.Token(ruolo: Ruolo.ADMIN));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Effetto collaterale voluto del profilo: UtenteCreateCommand registra l'accesso su pfw.Utenti.
    /// La tabella nasce vuota nel seed, quindi la riga che troviamo l'ha scritta la chiamata stessa.
    /// </summary>
    [Test]
    public async Task Profilo_ShouldRegistrareAccessoSuUtenti()
    {
        (await Get(_factory.Token(ruolo: Ruolo.ADMIN))).EnsureSuccessStatusCode();

        using var conn = new Microsoft.Data.SqlClient.SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pfw.Utenti";

        Assert.That((int)(await cmd.ExecuteScalarAsync())!, Is.GreaterThan(0),
            "La chiamata al profilo deve aver registrato l'accesso dell'utente.");
    }

    /// <summary>
    /// BE-AUTH-01: l'health check e' anonimo (AllowAnonymous) e in whitelist del nonce, quindi risponde
    /// senza credenziali. Vale piu' di quanto sembri: distingue "l'applicazione non parte" da "questo
    /// endpoint e' rotto" — se fallisce lui, ogni altro test rosso e' una conseguenza, non una causa.
    /// </summary>
    [Test]
    public async Task Health_SenzaCredenziali_ShouldReturn200()
    {
        var resp = await _factory.CreateClient().GetAsync("/health");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
