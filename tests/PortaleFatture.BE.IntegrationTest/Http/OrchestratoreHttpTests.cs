using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Le cinque rotte dell'area Orchestratore viste come le vede il client: routing, `[Authorize]`,
/// model binding e serializzazione. Copre BE-ORCH-01..09 del testbook backend.
///
/// Quattro rotte su cinque leggono `pfd.vOrchestratore` (lista, download, tipologie, fasi); la
/// quinta — `stati` — e' l'unica che non tocca il DB, perche' restituisce il dizionario hardcoded di
/// `StatiQuery`. E' anche il motivo per cui in UAT, dove la vista non esiste, la pagina non e' del
/// tutto muta: il filtro degli stati si popola e tutto il resto va in errore.
///
/// La copertura dei filtri/ordinamento/paginazione sta in OrchestratoreQueryIntegrationTests: qui
/// restano i comportamenti che **solo** un giro HTTP puo' mostrare — il binding dei due parametri di
/// paginazione, il 404 su lista vuota, e l'export Excel, che e' il punto in cui la reflection legge
/// `DescrizioneEsecuzione` (la proprieta' calcolata che fa `Esecuzione!.Value`).
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class OrchestratoreHttpTests
{
    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    // =============================================================================================
    // Lista
    // =============================================================================================

    [Test]
    public async Task Lista_ConPaginazione_ShouldReturn200()
    {
        var resp = await Post("/api/orchestratore?page=1&pageSize=50", "{}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.That(root.GetProperty("count").GetInt32(), Is.GreaterThan(0));
        Assert.That(root.GetProperty("items").EnumerateArray().Count(), Is.GreaterThan(0));
    }

    [Test]
    public async Task Lista_ShouldEsporreLaDescrizioneDelloStato_NonSoloIlCodice()
    {
        // `DescrizioneEsecuzione` non e' una colonna: e' calcolata al momento della serializzazione.
        // Se il getter esplodesse (Esecuzione NULL) sarebbe un 500 sull'intera risposta, non un campo
        // vuoto — motivo per cui vale la pena verificarla end-to-end e non solo a unit.
        var resp = await Post("/api/orchestratore?page=1&pageSize=50", "{}");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("descrizioneEsecuzione"));
        Assert.That(body, Does.Match("Programmato|Eseguito|Errore"));
    }

    [Test]
    public async Task Lista_PeriodoSenzaRighe_ShouldReturn404()
    {
        // Contratto dell'area (lo stesso di api/fatture): lista vuota => 404, non 200 con array vuoto.
        // Da sapere prima di aprire una segnalazione "l'Orchestratore mi da 404".
        var resp = await Post("/api/orchestratore?page=1&pageSize=50",
            """{ "init": "1990-01-01", "end": "1990-12-31" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Lista_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post("/api/orchestratore?page=1&pageSize=50", "{}", ruolo: null);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Lista_ConFiltri_ShouldRestringereIlRisultato()
    {
        var resp = await Post("/api/orchestratore?page=1&pageSize=50",
            """{ "tipologie": ["VAR. SEMESTRALE"] }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(3));
    }

    /// <summary>
    /// DIFETTO APERTO — omettere `page`/`pageSize` dalla query string produce un **500**, non un 400.
    ///
    /// La catena, verificata eseguendo il test: la rotta dichiara `[FromQuery] int page` e
    /// `[FromQuery] int pageSize` non nullable, quindi le minimal API rifiutano la richiesta **prima**
    /// di entrare nell'handler, con
    ///     `BadHttpRequestException: Required parameter "int page" was not provided from query string`
    /// — cioe' il framework la classifica correttamente come errore del **client**. E' il gestore di
    /// eccezioni globale dell'applicazione ad appiattirla poi in un 500, perdendo quella distinzione.
    ///
    /// Nota su cosa NON succede: la query non viene mai raggiunta, quindi qui non si arriva al
    /// `FETCH NEXT 0 ROWS` — a quello ci si arriva solo passando `pageSize=0` **esplicitamente**
    /// (v. SizeZero_… in OrchestratoreQueryIntegrationTests).
    ///
    /// L'aspettativa qui sotto e' quella corretta: chi ripara toglie l'[Ignore].
    /// </summary>
    [Test]
    [Ignore("DIFETTO APERTO — page/pageSize omessi danno 500 invece di 400: le minimal API sollevano "
        + "BadHttpRequestException (errore del client) e il gestore di eccezioni globale la traduce "
        + "in 500. Rimedio: gestire BadHttpRequestException come 400, oppure dichiarare i due "
        + "parametri nullable con un default. V. coverage/test-backlog.md.")]
    public async Task Lista_SenzaPageEPageSize_ShouldRispondereSenzaErroriDelServer()
    {
        var resp = await Post("/api/orchestratore", "{}");

        Assert.That((int)resp.StatusCode, Is.LessThan(500),
            "Un parametro mancante e' un errore del client, non del server.");
    }

    // =============================================================================================
    // Download
    // =============================================================================================

    [Test]
    public async Task Download_ShouldRestituireUnFileExcel()
    {
        // La rotta di download forza Page/Size a null: e' quindi anche l'unica che esercita da sola
        // il ramo "senza paginazione" della persistence.
        var resp = await Post("/api/orchestratore/download", "{}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.That(bytes, Is.Not.Empty);
        Assert.That(bytes.Take(2).ToArray(), Is.EqualTo(new byte[] { 0x50, 0x4B }).AsCollection,
            "Un .xlsx e' uno zip: deve iniziare con 'PK'. Se qui arriva altro, il file che l'utente "
            + "scarica non si apre in Excel.");
    }

    [Test]
    public async Task Download_PeriodoSenzaRighe_ShouldReturn404()
    {
        var resp = await Post("/api/orchestratore/download",
            """{ "init": "1990-01-01", "end": "1990-12-31" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // =============================================================================================
    // Dropdown
    // =============================================================================================

    [Test]
    public async Task Tipologie_ShouldReturn200_ConLeTipologieDellaVista()
    {
        var resp = await Get("/api/orchestratore/tipologie");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("PRIMO SALDO").And.Contain("IMPORT DATI"));
    }

    [Test]
    public async Task Fasi_ShouldReturn200_ConLeFasiDellaVista()
    {
        var resp = await Get("/api/orchestratore/fasi");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("REL").And.Contain("FINE CONT."));
    }

    [Test]
    public async Task Stati_ShouldReturn200_SenzaToccareIlDatabase()
    {
        // Gli stati sono hardcoded in StatiQuery, non letti da una lookup: e' l'unica rotta dell'area
        // che risponde anche dove la vista non esiste. Se il team Data introducesse un quinto stato,
        // la griglia mostrerebbe una descrizione vuota senza che nulla fallisca.
        var resp = await Get("/api/orchestratore/stati");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("Programmato"));
            Assert.That(body, Does.Contain("Eseguito no data"));
            Assert.That(body, Does.Contain("Errore"));
        });
    }

    // =============================================================================================
    // Helper
    // =============================================================================================

    private async Task<HttpResponseMessage> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(ConNonce(rotta), content);
        TestContext.Out.WriteLine($"POST {rotta} -> {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    private async Task<HttpResponseMessage> Get(string rotta, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var resp = await client.GetAsync(ConNonce(rotta));
        TestContext.Out.WriteLine($"GET {rotta} -> {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    /// <summary>
    /// Il nonce va in query string, quindi il separatore dipende da cosa c'e' gia' nella rotta: con un
    /// secondo '?' il middleware non lo troverebbe e la risposta sarebbe un 419 fuorviante.
    /// </summary>
    private string ConNonce(string rotta)
    {
        var separatore = rotta.Contains('?') ? "&" : "?";
        return $"{rotta}{separatore}nonce={Uri.EscapeDataString(_factory.Nonce())}";
    }
}
