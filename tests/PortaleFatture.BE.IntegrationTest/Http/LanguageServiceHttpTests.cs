using System.Net;
using System.Text;
using Azure.AI.TextAnalytics;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Le tre rotte di **Azure AI Language** (PF-775): `api/piid`, `api/language-detection`,
/// `api/summarize-text`.
///
/// **Cosa provano davvero questi test.** Che lo scenario "servizio non configurato" — quello che prima
/// del 02/09/2026 impediva l'avvio dell'intera applicazione (registrazione DI eager + costruttore che
/// sollevava: 206 test rossi su 568, tutti quelli che avviano l'app) — sia ora **contenuto**: l'API
/// parte, le altre rotte funzionano, e chi chiama queste tre riceve un **503** che dice perché.
///
/// Nessun test qui chiama Azure davvero: si usano fake di `ILanguageService`, oppure il servizio vero
/// costruito **senza credenziali**. Le chiamate reali stanno in `LanguageServiceRealeIntegrationTests`
/// e `LanguageServiceAdversarialIntegrationTests`, che sono `[Explicit]` e a pagamento.
///
/// ⚠️ **Lezione del 03/09/2026, gia' prevista dal commento che stava qui.** Questi test davano per
/// scontato che la macchina non avesse la sezione `PortaleFattureOptions:Language`, e su quell'assenza
/// poggiavano i tre casi del 503: un **verde ambientale**, non una proprietà del codice. Appena i
/// secrets sono stati configurati sono diventati rossi con un **502** — il servizio risultava
/// configurato e la chiamata partiva verso l'endpoint indicato. Ora la condizione è imposta dal test
/// stesso (`ClientCon(new LanguageService(null, null, ...))`), quindi l'esito non dipende più da come è
/// messa la macchina di chi esegue. Da tenere presente scrivendo altri test in quest'area: se un caso
/// passa "perché sull'ambiente manca qualcosa", non sta verificando ciò che sembra.
/// </summary>
public class LanguageServiceHttpTests
{
    private const string RottaPii = "/api/piid";
    private const string RottaLingua = "/api/language-detection";
    private const string RottaSintesi = "/api/summarize-text";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    // =============================================================================================
    // Il servizio non configurato non deve impedire l'avvio — è il difetto che ha motivato il fix
    // =============================================================================================

    [Test]
    public async Task ApiConLanguageNonConfigurato_ShouldAvviarsiComunque()
    {
        // Se l'applicazione non partisse, questa chiamata non otterrebbe alcuna risposta: e' la prova
        // diretta che una sezione di configurazione assente non fa piu' cadere il boot.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);

        var resp = await client.GetAsync("/health");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            "L'app deve avviarsi anche senza la sezione Language: un servizio esterno OPZIONALE non "
            + "puo' impedire il boot di cio' che non lo usa.");
    }

    [TestCase(RottaPii, TestName = "ServizioNonConfigurato_ShouldReturn503(api/piid)")]
    [TestCase(RottaLingua, TestName = "ServizioNonConfigurato_ShouldReturn503(api/language-detection)")]
    [TestCase(RottaSintesi, TestName = "ServizioNonConfigurato_ShouldReturn503(api/summarize-text)")]
    public async Task ServizioNonConfigurato_ShouldReturn503_ConMessaggio(string rotta)
    {
        // La condizione "non configurato" viene IMPOSTA qui, invece di dipendere dall'assenza della
        // sezione Language sulla macchina di chi esegue. Era un verde ambientale, ed e' diventato rosso
        // (502, non 200) il 03/09/2026 appena i secrets sono stati aggiunti: il servizio risultava
        // configurato e la chiamata partiva davvero.
        // Si usa il LanguageService VERO con endpoint e chiave nulli — non un fake — cosi' il test
        // continua a esercitare la logica di IsConfigured invece di una sua imitazione.
        var client = ClientCon(new LanguageService(null, null, NullLogger<LanguageService>.Instance));

        var (stato, corpo) = await Post(client, rotta, """{ "testo": "Mario Rossi, CF RSSMRA80A01H501U" }""");

        Assert.Multiple(() =>
        {
            Assert.That(stato, Is.EqualTo(HttpStatusCode.ServiceUnavailable),
                "503 e non 404: 'la funzione non e' disponibile qui' e' un fatto di ambiente, non "
                + "l'esito dell'elaborazione. E non 500: non e' un errore imprevisto.");
            Assert.That(corpo, Does.Contain("non e' configurato").IgnoreCase,
                "Il 503 deve dire perche': un corpo vuoto non aiuterebbe chi chiama.");
        });
    }

    // =============================================================================================
    // Validazione dell'input — precede la guardia, quindi si vede anche senza configurazione
    // =============================================================================================

    [TestCase("{}", TestName = "TestoAssente_ShouldReturn400(campo mancante)")]
    [TestCase("""{ "testo": "" }""", TestName = "TestoAssente_ShouldReturn400(stringa vuota)")]
    public async Task TestoAssente_ShouldReturn400(string body)
    {
        var (stato, _) = await Post(RottaPii, body);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.BadRequest),
            "Il testo e' obbligatorio, e il controllo viene prima della guardia sulla configurazione.");
    }

    /// <summary>
    /// ATTENZIONE`LanguageServiceRequest.testo` è un **campo pubblico**, non una proprietà, e in minuscolo:
    /// System.Text.Json lo deserializza **solo** grazie a `SerializerOptions.IncludeFields = true`,
    /// impostato una volta sola in `ConfigurationExtensions`. Se quella riga cambiasse, il binding
    /// smetterebbe di funzionare in silenzio e queste rotte risponderebbero 400 a ogni chiamata.
    ///
    /// Questo test lo blinda: se il campo smette di legare, il 400 arriva *prima* del 503 e il test
    /// diventa rosso indicando la causa.
    /// </summary>
    [Test]
    public async Task CampoTesto_ShouldEssereDeserializzato_NonostanteSiaUnCampo()
    {
        var (stato, _) = await Post(RottaPii, """{ "testo": "un testo qualsiasi" }""");

        Assert.That(stato, Is.Not.EqualTo(HttpStatusCode.BadRequest),
            "Il testo c'e': un 400 qui significherebbe che il binding del CAMPO 'testo' non funziona "
            + "piu' (IncludeFields disattivato), non che la richiesta e' malformata.");
    }

    // =============================================================================================
    // La distinzione fra "nessun risultato" e "il servizio a monte ha fallito"
    //
    // Prima entrambi i casi tornavano `null` dal servizio e l'endpoint li traduceva in **404**: per il
    // client, "in questo testo non ci sono PII" e "la chiave Azure e' scaduta" erano la stessa cosa.
    // Ora l'errore del servizio esterno solleva `UpstreamServiceException` → **502**.
    //
    // Qui il servizio reale viene sostituito con due fake — non serve una chiave Azure, e soprattutto
    // il test verifica la REGOLA (quale esito produce quale codice) invece del comportamento di un
    // servizio esterno che non controlliamo.
    // =============================================================================================

    [Test]
    public async Task ServizioCheFallisce_ShouldReturn502_ENon404()
    {
        var client = ClientCon(new LanguageServiceFake(fallisce: true));

        var resp = await client.PostAsync(_factory.WithNonce(RottaPii),
            new StringContent("""{ "testo": "Mario Rossi" }""", Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"servizio in errore -> {(int)resp.StatusCode} | {corpo}");

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway),
                "Un guasto del servizio a monte non deve travestirsi da 404: il chiamante non puo' "
                + "distinguerlo da 'nessuna PII trovata' e riproverebbe cambiando input, inutilmente.");
            Assert.That(corpo, Does.Contain("Azure AI Language"),
                "Il 502 deve dire quale servizio ha fallito.");
        });
    }

    [Test]
    public async Task ServizioInTimeout_ShouldReturn504_ENon502()
    {
        // Il 504 e' una UpstreamTimeoutException, che DERIVA da UpstreamServiceException: nel gestore
        // globale deve essere elencata prima della base, altrimenti il pattern matching la cattura e
        // risponde 502. Questo test protegge proprio quell'ordine, che e' facile invertire per sbaglio
        // riordinando lo switch.
        var client = ClientCon(new LanguageServiceFake(fallisce: true, inTimeout: true));

        var resp = await client.PostAsync(_factory.WithNonce(RottaSintesi),
            new StringContent("""{ "testo": "un testo lungo da sintetizzare" }""", Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"servizio in timeout -> {(int)resp.StatusCode} | {corpo}");

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.GatewayTimeout),
                "504 e non 502: 'non ha risposto in tempo' e 'ha risposto male' hanno cause e rimedi "
                + "diversi. Se qui arriva 502, nello switch la classe base precede la derivata.");
            Assert.That(corpo, Does.Contain("secondi").IgnoreCase,
                "Il messaggio deve dire entro quanto tempo ci si aspettava la risposta.");
        });
    }

    [Test]
    public async Task NessunRisultato_ShouldRestare404()
    {
        // Contro-prova: il 404 resta per il suo significato legittimo. Senza questa, il test sopra
        // proverebbe solo che "qualcosa da' 502", non che i due casi sono DISTINTI.
        var client = ClientCon(new LanguageServiceFake(fallisce: false));

        var resp = await client.PostAsync(_factory.WithNonce(RottaPii),
            new StringContent("""{ "testo": "nessun dato personale qui" }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
            "Elaborazione riuscita, nessuna PII: 404, ed e' corretto.");
    }

    /// <summary>
    /// La risposta della sintesi è il **contratto nostro**, non il tipo dell'SDK Azure.
    ///
    /// Prima la rotta restituiva `List&lt;AbstractiveSummarizeResultCollection&gt;` direttamente: il JSON
    /// visto dal frontend era la forma interna di `Azure.AI.TextAnalytics` — statistiche, versione del
    /// modello, offset — e un aggiornamento del pacchetto avrebbe potuto cambiarlo senza che nulla
    /// fallisse da noi. Ora esce `{ "sintesi": [ … ] }`.
    ///
    /// Il test guarda il JSON e non l'oggetto tipizzato proprio perché è il JSON il contratto.
    /// </summary>
    [Test]
    public async Task Sintesi_ShouldRestituireIlContrattoNostro_NonIlTipoDellSdk()
    {
        var client = ClientCon(new LanguageServiceFake(fallisce: false, sintesi: ["Prima sintesi.", "Seconda sintesi."]));

        var resp = await client.PostAsync(_factory.WithNonce(RottaSintesi),
            new StringContent("""{ "testo": "un testo da sintetizzare" }""", Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"sintesi -> {(int)resp.StatusCode} | {corpo}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(corpo, Does.Contain("sintesi").IgnoreCase, "La chiave del contratto nostro.");
            Assert.That(corpo, Does.Contain("Prima sintesi.").And.Contain("Seconda sintesi."),
                "Tutte le sintesi devono arrivare: appiattirle a una sola perderebbe informazione.");
            Assert.That(corpo, Does.Not.Contain("modelVersion").IgnoreCase,
                "Nessun campo interno dell'SDK deve trapelare nel contratto pubblico.");
            Assert.That(corpo, Does.Not.Contain("statistics").IgnoreCase);
        });
    }

    [Test]
    public async Task Sintesi_SenzaRisultati_ShouldReturn404()
    {
        // Elaborazione riuscita ma nessuna sintesi prodotta: 404, coerente con le altre due rotte.
        var client = ClientCon(new LanguageServiceFake(fallisce: false, sintesi: []));

        var resp = await client.PostAsync(_factory.WithNonce(RottaSintesi),
            new StringContent("""{ "testo": "x" }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Client con `ILanguageService` sostituito da un fake.</summary>
    private HttpClient ClientCon(ILanguageService fake)
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ILanguageService>();
            s.AddSingleton(fake);
        })).CreateClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, Ruolo.ADMIN);
        return client;
    }

    /// <summary>
    /// Fake minimo: o fallisce come farebbe il servizio esterno (quota, credenziale, rete), o riesce
    /// senza produrre risultati. Sono i due esiti che prima collassavano entrambi in un 404.
    /// </summary>
    private sealed class LanguageServiceFake(bool fallisce, bool inTimeout = false, string[]? sintesi = null) : ILanguageService
    {
        public bool IsConfigured => true;

        private Exception Errore(string operazione) => inTimeout
            ? new UpstreamTimeoutException($"Azure AI Language: {operazione} non completata entro 45 secondi.")
            : new UpstreamServiceException($"Azure AI Language: {operazione} non riuscita.");

        public Task<PiiEntityCollection?> DetectPersonalIdentifiableInformationAsync(
            string text, string language = "it", CancellationToken cancellationToken = default) =>
            fallisce
                ? throw Errore("rilevazione PII")
                : Task.FromResult<PiiEntityCollection?>(null);

        public Task<DetectedLanguage?> DetectLanguageAsync(
            string text, CancellationToken cancellationToken = default) =>
            fallisce
                ? throw Errore("rilevazione della lingua")
                : Task.FromResult<DetectedLanguage?>(null);

        public Task<List<AbstractiveSummarizeResultCollection>?> SummarizeTextAsync(
            string text, string language = "it", CancellationToken cancellationToken = default)
        {
            if (fallisce) throw Errore("sintesi del testo");
            if (sintesi is null) return Task.FromResult<List<AbstractiveSummarizeResultCollection>?>(null);

            // I tipi dell'SDK non hanno costruttori pubblici: si compongono con le factory di
            // TextAnalyticsModelFactory, che Azure espone proprio per i test.
            var risultato = TextAnalyticsModelFactory.AbstractiveSummarizeResult(
                id: "0",
                statistics: default,
                summaries: [.. sintesi.Select(t => TextAnalyticsModelFactory.AbstractiveSummary(t, []))],
                warnings: []);

            return Task.FromResult<List<AbstractiveSummarizeResultCollection>?>(
                [TextAnalyticsModelFactory.AbstractiveSummarizeResultCollection([risultato], statistics: null, modelVersion: "test")]);
        }
    }

    // =============================================================================================
    // Limite di lunghezza del testo (limite di Azure, applicato PRIMA della chiamata)
    // =============================================================================================

    /// <summary>
    /// Un testo oltre il limite dev'essere respinto **prima** di chiamare Azure: **400**, non 502.
    ///
    /// Senza il controllo il limite lo applicava il servizio esterno — l'SDK sollevava, il catch
    /// traduceva in `UpstreamServiceException` e il client vedeva un 502, cioè "il servizio a monte ha
    /// un problema" quando invece il problema era nella sua richiesta. E la chiamata veniva **fatta e
    /// pagata** prima di scoprirlo.
    ///
    /// Il servizio reale non è configurato in test, quindi qui si userebbe il 503; si passa perciò da
    /// un fake configurato, per far arrivare la richiesta al controllo di lunghezza. Il fake però non
    /// implementa il limite (lo fa `LanguageService`), quindi il test verifica la **rotta con il
    /// servizio vero**: v. `LimiteLunghezza_UnitSulServizio` nel progetto unit.
    /// </summary>
    [Test]
    public async Task TestoOltreIlLimite_ShouldReturn400_ENon502()
    {
        // 6.000 caratteri: sopra il limite delle operazioni sincrone (5.120), sotto quello della sintesi.
        var testoLungo = new string('a', 6_000);
        var client = ClientCon(LanguageServiceReale());

        var resp = await client.PostAsync(_factory.WithNonce(RottaPii),
            new StringContent($$"""{ "testo": "{{testoLungo}}" }""", Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"testo 6000 caratteri -> {(int)resp.StatusCode}");

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                "Il testo troppo lungo e' un errore della RICHIESTA (400), non un guasto del servizio "
                + "a monte (502): e la chiamata ad Azure non deve nemmeno partire.");
            Assert.That(corpo, Does.Contain("5.120").Or.Contain("5120"),
                "Il messaggio deve dire qual e' il limite e quanto era lungo il testo ricevuto.");
        });
    }

    [Test]
    public async Task TestoEntroIlLimite_ShouldSuperareIlControllo()
    {
        // Contro-prova: 5.000 caratteri passano il controllo e arrivano alla chiamata (che qui fallisce
        // perche' il servizio finto non ha credenziali: l'importante e' che NON sia un 400).
        var testo = new string('a', 5_000);
        var client = ClientCon(LanguageServiceReale());

        var resp = await client.PostAsync(_factory.WithNonce(RottaPii),
            new StringContent($$"""{ "testo": "{{testo}}" }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.BadRequest),
            "5.000 caratteri sono sotto il limite: il controllo di lunghezza non deve scattare.");
    }

    /// <summary>
    /// Il `LanguageService` **vero**, costruito con endpoint e chiave fittizi: `IsConfigured` è `true`,
    /// quindi la guardia del 503 non scatta e la richiesta arriva ai controlli veri del servizio —
    /// che è esattamente ciò che questi due test devono esercitare. La chiamata ad Azure non parte
    /// perché il testo viene respinto prima (primo test) o perché l'endpoint è fittizio (secondo).
    /// </summary>
    private static LanguageService LanguageServiceReale() => new(
        endpoint: "https://esempio-non-raggiungibile.cognitiveservices.azure.com/",
        key: "chiave-fittizia-per-test",
        logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<LanguageService>.Instance);

    // =============================================================================================
    // Autorizzazione
    // =============================================================================================

    [Test]
    public async Task SenzaAutenticazione_ShouldReturn401()
    {
        var (stato, _) = await Post(RottaPii, """{ "testo": "x" }""", ruolo: null);

        Assert.That(stato, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ConTokenEnte_ShouldReturn403()
    {
        // Le tre rotte sono PagoPAPolicy: un aderente SelfCare non deve raggiungerle.
        var client = _factory.CreateClientAs(Ruolo.ADMIN, auth: "SELFCARE", profilo: "PA");
        var resp = await client.PostAsync(_factory.WithNonce(RottaPii),
            new StringContent("""{ "testo": "x" }""", Encoding.UTF8, "application/json"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // =============================================================================================

    private Task<(HttpStatusCode stato, string corpo)> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN)
        => Post(_factory.CreateClientAs(ruolo), rotta, body);

    /// <summary>Variante per i test che devono imporre una specifica registrazione di ILanguageService.</summary>
    private async Task<(HttpStatusCode stato, string corpo)> Post(HttpClient client, string rotta, string body)
    {
        var resp = await client.PostAsync(_factory.WithNonce(rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));
        var corpo = await resp.Content.ReadAsStringAsync();
        TestContext.Out.WriteLine($"POST {rotta} -> {(int)resp.StatusCode} {resp.StatusCode} | {corpo}");
        return (resp.StatusCode, corpo);
    }
}
