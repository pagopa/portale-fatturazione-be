using System.Net;

namespace PortaleFatture.BE.IntegrationTest;

/// <summary>
/// Integration API (Function.API) eseguita dall'host vero, in container: la stessa immagine che si
/// pubblica. Fino al 13/11/2026 l'Integration API aveva solo unit test con il middleware isolato;
/// qui si verifica cio' che quelli non possono vedere — che l'host parta, che le function siano
/// indicizzate, e che la catena API key → ente abilitato → whitelist IP funzioni contro il DB.
///
/// E' anche il controllo da rilanciare dopo un aggiornamento di pacchetti o dell'immagine di base:
/// se l'host non parte o una function non viene indicizzata, qui si vede subito.
///
/// Limite dichiarato: il percorso felice (chiamata autorizzata che risponde 200) richiede la tabella
/// pfw.ApiLog, su cui l'API scrive a ogni chiamata autenticata e che il seed non ha ancora. Quei test
/// restano gialli (ignorati) finche' la DDL reale non entra in tests/Data.
/// </summary>
[Category("IntegrationApi")]
[NonParallelizable]
public class IntegrationApiHostTests
{
    private static readonly string Chiave = "test-host-" + Guid.NewGuid().ToString("N");
    private static readonly string ChiaveEnteNonAbilitato = "test-host-ko-" + Guid.NewGuid().ToString("N");

    private const string Autenticazione = "/api/v1/authentication";

    [OneTimeSetUp]
    public void Prepara()
    {
        IntegrationApiHost.SkipIfUnavailable();
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        Pulisci();
        IntegrationApiHost.InserisciChiave(IntegrationApiHost.EnteAbilitato, Chiave);
        IntegrationApiHost.InserisciIp(IntegrationApiHost.EnteAbilitato, IntegrationApiHost.IpAutorizzato);
        IntegrationApiHost.InserisciChiave(IntegrationApiHost.EnteNonAbilitato, ChiaveEnteNonAbilitato);
    }

    [OneTimeTearDown]
    public void Pulisci() =>
        IntegrationApiHost.Pulisci([Chiave, ChiaveEnteNonAbilitato], [IntegrationApiHost.IpAutorizzato]);

    private static Task<HttpResponseMessage> Get(string percorso, string? apiKey = null, string? ip = null, string? ipCustom = null)
        => IntegrationApiHost.Invia(HttpMethod.Get, percorso, apiKey, ip, ipCustom);

    // ------------------------------------------------------------------ l'host parte e indicizza

    /// <summary>
    /// Una rotta per area, GET e POST: senza chiave rispondono tutte 401. Un 404 direbbe che la
    /// function non e' stata indicizzata, un 500 che l'host o il middleware non partono.
    /// </summary>
    [TestCase("GET", "/api/v1/authentication")]
    [TestCase("GET", "/api/v1/modulocommessa/periodo")]
    [TestCase("GET", "/api/v1/modulocommessa/regioni")]
    [TestCase("GET", "/api/v1/notifiche/periodo")]
    [TestCase("GET", "/api/v1/contestazioni/tipologia")]
    [TestCase("GET", "/api/v1/fatture/periodo")]
    [TestCase("GET", "/api/v1/rel/periodo")]
    [TestCase("POST", "/api/v1/fatture/ricerca")]
    [TestCase("POST", "/api/v1/rel/testata")]
    [TestCase("POST", "/api/v1/notifiche")]
    [TestCase("POST", "/api/v1/modulocommessa")]
    public async Task Rotta_SenzaApiKey_ShouldEssereIndicizzataERispondere401(string metodo, string percorso)
    {
        var risposta = await IntegrationApiHost.Invia(new HttpMethod(metodo), percorso, corpo: metodo == "POST" ? "{}" : null);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RottaInesistente_ShouldRispondere404()
    {
        var risposta = await Get("/api/v1/rotta-che-non-esiste", Chiave, IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ------------------------------------------------------------------ API key

    [Test]
    public async Task ChiaveSconosciuta_ShouldRispondere401()
    {
        var risposta = await Get(Autenticazione, "chiave-mai-emessa", IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>La chiave esiste ma l'ente non e' fra quelli abilitati alle Integration API: 401, non 403.</summary>
    [Test]
    public async Task ChiaveDiEnteNonAbilitato_ShouldRispondere401()
    {
        var risposta = await Get(Autenticazione, ChiaveEnteNonAbilitato, IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // ------------------------------------------------------------------ whitelist IP

    [Test]
    public async Task ChiaveValida_IpNonInWhitelist_ShouldRispondere403()
    {
        var risposta = await Get(Autenticazione, Chiave, IntegrationApiHost.IpEstraneo);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Nessuna intestazione inoltrata: in Release non c'e' ripiego su 127.0.0.1, quindi l'indirizzo e'
    /// sconosciuto e la chiamata e' respinta. E' cio' che succederebbe aggirando il gateway.
    /// </summary>
    [Test]
    public async Task ChiaveValida_SenzaIndirizzoInoltrato_ShouldRispondere403()
    {
        var risposta = await Get(Autenticazione, Chiave);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Caratterizzazione della precedenza: Custom-Forwarded-For vince su X-Forwarded-For. Mettere
    /// l'indirizzo autorizzato solo nella seconda intestazione non basta.
    /// </summary>
    [Test]
    public async Task ChiaveValida_CustomForwardedForEstraneo_ShouldVincereSuXForwardedFor()
    {
        var risposta = await Get(Autenticazione, Chiave, ip: IntegrationApiHost.IpAutorizzato, ipCustom: IntegrationApiHost.IpEstraneo);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>Di una catena di proxy conta il primo indirizzo: l'autorizzato in coda non apre la porta.</summary>
    [Test]
    public async Task ChiaveValida_IpAutorizzatoInCodaAllaCatena_ShouldRispondere403()
    {
        var risposta = await Get(Autenticazione, Chiave, $"{IntegrationApiHost.IpEstraneo}, {IntegrationApiHost.IpAutorizzato}");

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // ------------------------------------------------------------------ adversarial sulla chiave

    [TestCase("' OR '1'='1")]
    [TestCase("'; DROP TABLE pfw.ApiKeys; --")]
    [TestCase("%")]
    [TestCase("*")]
    [TestCase(" ")]
    [TestCase("null")]
    public async Task ChiaveOstile_ShouldRispondere401_SenzaErrori(string chiave)
    {
        var risposta = await Get(Autenticazione, chiave, IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ChiaveMoltoLunga_ShouldRispondere401_SenzaErrori()
    {
        var risposta = await Get(Autenticazione, new string('k', 6000), IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>La chiave vale solo come intestazione: in query string non autentica.</summary>
    [Test]
    public async Task ChiaveInQueryString_ShouldNonAutenticare()
    {
        var risposta = await Get($"{Autenticazione}?x-api-key={Chiave}&apikey={Chiave}", ip: IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Dopo i tentativi ostili la tabella delle chiavi c'e' ancora e la chiave buona e' ancora riconosciuta.</summary>
    [Test]
    public async Task DopoITentativiOstili_LaChiaveBuonaEAncoraRiconosciuta()
    {
        await Get(Autenticazione, "'; DROP TABLE pfw.ApiKeys; --", IntegrationApiHost.IpAutorizzato);

        var risposta = await Get(Autenticazione, Chiave, IntegrationApiHost.IpEstraneo);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), "403 = chiave riconosciuta, IP respinto");
    }

    // ------------------------------------------------------------------ percorso felice (serve pfw.ApiLog)

    /// <summary>
    /// Chiave valida da un indirizzo in whitelist: 200 con l'esito dell'autenticazione. La rotta scrive
    /// in pfw.ApiLog: finche' la tabella non e' nel seed il test e' ignorato, non rosso.
    /// </summary>
    [Test]
    public async Task ChiaveValida_IpInWhitelist_ShouldRispondere200()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfw.ApiLog");

        var risposta = await Get(Autenticazione, Chiave, IntegrationApiHost.IpAutorizzato);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.OK), await risposta.Content.ReadAsStringAsync());
    }

    /// <summary>L'intestazione della chiave non distingue maiuscole e minuscole.</summary>
    [Test]
    public async Task ChiaveValida_IntestazioneInMinuscolo_ShouldRispondere200()
    {
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "pfw.ApiLog");
        var richiesta = new HttpRequestMessage(HttpMethod.Get, IntegrationApiHost.BaseUrl + Autenticazione);
        richiesta.Headers.TryAddWithoutValidation("x-api-key", Chiave);
        richiesta.Headers.TryAddWithoutValidation("x-forwarded-for", IntegrationApiHost.IpAutorizzato);
        using var client = new HttpClient();

        var risposta = await client.SendAsync(richiesta);

        Assert.That(risposta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
