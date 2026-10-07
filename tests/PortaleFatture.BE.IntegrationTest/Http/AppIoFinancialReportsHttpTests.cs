using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte api/appio/financialreports* (PF-908): filtri Anno / Trimestre, griglia dei documenti
/// contabili e i due download ("Documenti Contabili" e "Financial Report"). Leggono
/// [be].[vwAppioFinancialReportPositions], [be].[vwAppioFinancialReport] e [be].[vwAppioContracts].
///
/// Seed (tests/Data/appio.sql): posizioni in 2026_2 (APPIO-FR-001 di C1 con due righe,
/// APPIO-FR-002 di C2), 2026_1 (APPIO-FR-003 di C1) e 2025_4 (APPIO-FR-004 di C5, senza contratto
/// né report). C2 non ha financial report; C3 ha un financial report in 2026_2 ma nessuna posizione.
/// </summary>
public class AppIoFinancialReportsHttpTests
{
    private const string Base = "/api/appio/financialreports";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReportPositions");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinancialReport");
    }

    /// <summary>
    /// POST con il ruolo indicato (null = anonimo) e nonce valido.
    /// </summary>
    private async Task<HttpResponseMessage> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    /// <summary>
    /// Corpo della risposta come JSON.
    /// </summary>
    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    /// <summary>
    /// Numeri dei documenti della griglia.
    /// </summary>
    private static List<string> Numeri(JsonElement griglia)
        => griglia.GetProperty("financialReports").EnumerateArray().Select(x => x.GetProperty("numero").GetString()!).ToList();

    /// <summary>
    /// Fogli di un xlsx (nome -> testo del foglio). xl/workbook.xml dà nome e r:id di ogni foglio,
    /// xl/_rels/workbook.xml.rels traduce l'r:id nel file: l'ordine dei file non segue quello dei
    /// fogli, e nei .rels l'ordine degli attributi non è garantito, quindi si leggono uno per uno.
    /// </summary>
    private static async Task<Dictionary<string, string>> Fogli(HttpResponseMessage resp)
    {
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        string Leggi(string path) => new StreamReader(zip.GetEntry(path)!.Open()).ReadToEnd();
        static string Attributo(string elemento, string nome) => Regex.Match(elemento, $"\\b{nome}=\"([^\"]+)\"").Groups[1].Value;

        var targets = Regex.Matches(Leggi("xl/_rels/workbook.xml.rels"), "<[^>]*Relationship [^>]*>")
            .ToDictionary(m => Attributo(m.Value, "Id"), m => Attributo(m.Value, "Target").TrimStart('/'));

        return Regex.Matches(Leggi("xl/workbook.xml"), "<[^>]*sheet [^>]*>")
            .ToDictionary(
                m => Attributo(m.Value, "name"),
                m =>
                {
                    var target = targets[Attributo(m.Value, "r:id")];
                    return Leggi(target.StartsWith("xl/") ? target : "xl/" + target);
                });
    }

    // --- years / quarters ---

    /// <summary>
    /// Gli anni vengono dai trimestri in cui ci sono posizioni, dal più recente.
    /// </summary>
    [Test]
    public async Task Years_ShouldRestituireGliAnniDellePosizioni()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(_factory.WithNonce($"{Base}/years"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var anni = (await Json(resp)).EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(anni, Is.EqualTo(new[] { "2026", "2025" }));
    }

    /// <summary>
    /// I trimestri dell'anno richiesto, con etichetta "Qn", in ordine crescente.
    /// </summary>
    [Test]
    public async Task Quarters_ShouldRestituireITrimestriDellAnno()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var voci = (await Json(resp)).EnumerateArray().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(voci.Select(x => x.GetProperty("value").GetString()), Is.EqualTo(new[] { "2026_1", "2026_2" }));
            Assert.That(voci.Select(x => x.GetProperty("quarter").GetString()), Is.EqualTo(new[] { "Q1", "Q2" }));
        });
    }

    /// <summary>
    /// Un anno senza documenti risponde 404.
    /// </summary>
    [Test]
    public async Task Quarters_AnnoSenzaDocumenti_ShouldReturn404()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "1999" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- griglia ---

    /// <summary>
    /// Senza filtri si vede il trimestre più recente: due documenti, e il conteggio è sui documenti,
    /// non sulle righe (APPIO-FR-001 ne ha due).
    /// </summary>
    [Test]
    public async Task Griglia_SenzaFiltri_ShouldRestituireIDocumentiDelTrimestrePiuRecente()
    {
        var resp = await Post(Base, "{}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var griglia = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Numeri(griglia), Is.EqualTo(new[] { "APPIO-FR-001", "APPIO-FR-002" }));
            Assert.That(griglia.GetProperty("count").GetInt32(), Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Le righe dello stesso documento diventano le sue posizioni, in ordine di progressivo, con la
    /// categoria presa dal financial report; nome e chiave della testata dal report e dal documento.
    /// </summary>
    [Test]
    public async Task Griglia_ShouldRaggrupparePosizioniPerDocumento()
    {
        var resp = await Post(Base, """{ "quarters": ["2026_2"], "contractIds": ["APPIO-C1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var doc = (await Json(resp)).GetProperty("financialReports").EnumerateArray().Single();
        var posizioni = doc.GetProperty("posizioni").EnumerateArray().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(doc.GetProperty("key").GetString(), Is.EqualTo("APPIO-C1|2026_2|APPIO-FR-001"));
            Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo("Comune Alfa AppIO FR"), "il nome viene dal financial report");
            Assert.That(doc.GetProperty("yearQuarter").GetString(), Is.EqualTo("2026_2"));
            Assert.That(posizioni.Select(p => p.GetProperty("progressivoRiga").GetInt32()), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(posizioni.Select(p => p.GetProperty("codiceArticolo").GetString()), Is.EqualTo(new[] { "ART-A", "ART-B" }));
            Assert.That(posizioni.Select(p => p.GetProperty("category").GetString()), Is.EqualTo(new[] { "CAT-MSG", "CAT-PAG" }));
            Assert.That(posizioni.Select(p => p.GetProperty("importo").GetDecimal()), Is.EqualTo(new[] { 10m, 10m }));
        });
    }

    /// <summary>
    /// Senza financial report il documento c'è lo stesso (le posizioni guidano): nome dal contratto,
    /// categoria nulla.
    /// </summary>
    [Test]
    public async Task Griglia_SenzaFinancialReport_ShouldPrendereIlNomeDalContratto()
    {
        var resp = await Post(Base, """{ "quarters": ["2026_2"], "contractIds": ["APPIO-C2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var doc = (await Json(resp)).GetProperty("financialReports").EnumerateArray().Single();
        Assert.Multiple(() =>
        {
            Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo("Comune Beta AppIO Test"));
            Assert.That(doc.GetProperty("posizioni")[0].GetProperty("category").ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    /// <summary>
    /// Senza contratto né report il nome resta nullo, e senza partita IVA si usa il codice fiscale.
    /// </summary>
    [Test]
    public async Task Griglia_SenzaContrattoNeReport_ShouldAvereNomeNulloEVatCodeDalCodiceFiscale()
    {
        var resp = await Post(Base, """{ "quarters": ["2025_4"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var doc = (await Json(resp)).GetProperty("financialReports").EnumerateArray().Single();
        Assert.Multiple(() =>
        {
            Assert.That(doc.GetProperty("contractId").GetString(), Is.EqualTo("APPIO-C5"));
            Assert.That(doc.GetProperty("name").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(doc.GetProperty("vatCode").GetString(), Is.EqualTo("00000000005"));
        });
    }

    /// <summary>
    /// Con il solo anno si cercano tutti i trimestri dell'anno; i trimestri, se ci sono, prevalgono.
    /// </summary>
    [TestCase("""{ "year": "2026" }""", new[] { "APPIO-FR-003", "APPIO-FR-001", "APPIO-FR-002" })]
    [TestCase("""{ "year": "2026", "quarters": ["2026_1"] }""", new[] { "APPIO-FR-003" })]
    [TestCase("""{ "year": "2025" }""", new[] { "APPIO-FR-004" })]
    public async Task Griglia_FiltroAnnoETrimestri(string body, string[] attesi)
    {
        var resp = await Post(Base, body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Numeri(await Json(resp)), Is.EquivalentTo(attesi));
    }

    /// <summary>
    /// Un contratto con financial report ma senza posizioni (C3) non compare in griglia.
    /// </summary>
    [Test]
    public async Task Griglia_ContrattoSoloNelFinancialReport_ShouldReturn404()
    {
        var resp = await Post(Base, """{ "quarters": ["2026_2"], "contractIds": ["APPIO-C3"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Il valore del filtro resta un parametro: un tentativo di injection non trova nulla e non
    /// tocca le tabelle.
    /// </summary>
    [Test]
    public async Task Griglia_InjectionNeiFiltri_ShouldEssereTrattataComeTesto()
    {
        var resp = await Post(Base, """{ "year": "2026' OR 1=1; DROP TABLE appio.FinancialReportPositions;--", "contractIds": ["x' OR '1'='1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var ancora = await Post(Base, """{ "year": "2026" }""");
        Assert.That(ancora.StatusCode, Is.EqualTo(HttpStatusCode.OK), "la tabella delle posizioni è ancora lì");
    }

    /// <summary>
    /// Senza autenticazione 401.
    /// </summary>
    [Test]
    public async Task Griglia_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(Base, "{}", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- download "Documenti Contabili" ---

    /// <summary>
    /// Un foglio "Documenti Contabili" con una riga per posizione, con lo stesso filtro della griglia.
    /// </summary>
    [Test]
    public async Task Document_ShouldEsportareUnaRigaPerPosizione()
    {
        var resp = await Post($"{Base}/document", """{ "quarters": ["2026_2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        var fogli = await Fogli(resp);
        Assert.That(fogli.Keys, Is.EqualTo(new[] { "Documenti Contabili" }));
        var testo = fogli["Documenti Contabili"];
        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(testo, "APPIO-FR-001").Count, Is.EqualTo(2), "due righe per il documento con due posizioni");
            Assert.That(testo, Does.Contain("APPIO-FR-002"));
            Assert.That(testo, Does.Not.Contain("APPIO-FR-003"), "2026_1 è fuori dal filtro");
        });
    }

    /// <summary>
    /// Filtro senza risultati: 404, nessun file.
    /// </summary>
    [Test]
    public async Task Document_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/document", """{ "quarters": ["1999_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- download "Financial Report" ---

    /// <summary>
    /// Per ogni trimestre un foglio financial-report e uno positions. Il foglio del report contiene
    /// anche C3, che ha il report ma nessuna posizione: è la differenza rispetto alla griglia.
    /// </summary>
    [Test]
    public async Task DetailDownload_ShouldAvereIFogliReportEPosizioniPerTrimestre()
    {
        var resp = await Post($"{Base}/detail/download", """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fogli = await Fogli(resp);
        Assert.That(fogli.Keys, Is.EquivalentTo(new[]
        {
            "2026-q1-financial-report", "2026-q2-financial-report", "2026-q1-positions", "2026-q2-positions"
        }));
        Assert.Multiple(() =>
        {
            Assert.That(fogli["2026-q2-financial-report"], Does.Contain("APPIO-C3"));
            Assert.That(fogli["2026-q2-positions"], Does.Not.Contain("APPIO-C3"));
            Assert.That(fogli["2026-q2-positions"], Does.Contain("APPIO-FR-002"));
            Assert.That(fogli["2026-q1-positions"], Does.Contain("APPIO-FR-003"));
        });
    }

    /// <summary>
    /// Il filtro per contratto vale per entrambi i fogli, anche se nel report la colonna si chiama
    /// recipient_id.
    /// </summary>
    [Test]
    public async Task DetailDownload_FiltroContratto_ShouldValerePerEntrambiIFogli()
    {
        var resp = await Post($"{Base}/detail/download", """{ "quarters": ["2026_2"], "contractIds": ["APPIO-C2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var fogli = await Fogli(resp);
        Assert.Multiple(() =>
        {
            Assert.That(fogli.Keys, Is.EqualTo(new[] { "2026-q2-positions" }), "C2 non ha financial report: niente foglio del report");
            Assert.That(fogli["2026-q2-positions"], Does.Not.Contain("APPIO-C1"));
        });
    }

    /// <summary>
    /// Filtro senza risultati in nessuna delle due viste: 404, nessun file.
    /// </summary>
    [Test]
    public async Task DetailDownload_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/detail/download", """{ "quarters": ["1999_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
