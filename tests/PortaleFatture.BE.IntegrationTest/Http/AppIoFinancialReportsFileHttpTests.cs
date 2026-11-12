using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// End-to-end della rotta POST api/appio/financialreports/blob/download (12/11/2026): scarica il file
/// del financial report di un trimestre. Il percorso viene da file_path di [be].[vwAppioFinalReport]
/// e resta nel backend; il blob storage è sostituito da <see cref="FakeAppIoFinancialReportStorageService"/>.
///
/// Seed (tests/Data/appio.sql): un file per trimestre, file_path ripetuto uguale su tutte le righe,
/// nella forma abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/AAAA/T/appio-final_report-AAAA-qT.csv.
/// Trimestri con documenti: 2026_2, 2026_1, 2025_4.
/// </summary>
[Category("AppIO")]
public class AppIoFinancialReportsFileHttpTests
{
    private const string Rotta = "/api/appio/financialreports/blob/download";
    private const string Path2026Q2 = "abfss://appioexport@stappioseed.dfs.core.windows.net/export/sap/2026/2/appio-final_report-2026-q2.csv";

    private ApiTestFactory _factory = null!;
    private static FakeAppIoFinancialReportStorageService Storage => FakeAppIoFinancialReportStorageService.Instance;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReport");
        Storage.Reset();
    }

    [TearDown]
    public void ResetStorage() => Storage.Reset();

    private async Task<HttpResponseMessage> Post(string body, string? ruolo = Ruolo.ADMIN, string prodottoNonce = "prod-pn")
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(Rotta, prodotto: prodottoNonce), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    // --- percorso felice ---

    /// <summary>
    /// Anno e trimestre con documenti: 200 con il contenuto del file, tipo text/csv e un nome
    /// costruito dal trimestre. Al servizio di storage arriva il file_path di quel trimestre.
    /// </summary>
    [Test]
    public async Task Download_TrimestreConDocumenti_ShouldRestituireIlFile()
    {
        var resp = await Post("""{ "year": 2026, "quarter": 2 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(async () =>
        {
            Assert.That(await resp.Content.ReadAsStringAsync(), Is.EqualTo(FakeAppIoFinancialReportStorageService.Contenuto));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_2.csv"));
            Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
        });
    }

    /// <summary>
    /// Ogni trimestre ha il suo file: il percorso chiesto allo storage cambia con anno e trimestre.
    /// </summary>
    [TestCase(2026, 1, "export/sap/2026/1/appio-final_report-2026-q1.csv")]
    [TestCase(2025, 4, "export/sap/2025/4/appio-final_report-2025-q4.csv")]
    public async Task Download_AltriTrimestri_ShouldChiedereIlFileDelTrimestre(int year, int quarter, string coda)
    {
        var resp = await Post($$"""{ "year": {{year}}, "quarter": {{quarter}} }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Storage.Richieste.Single(), Does.EndWith(coda));
    }

    /// <summary>
    /// Il percorso sul blob storage non esce dal backend: né nelle intestazioni né nel corpo della
    /// risposta compaiono account, container o cartelle.
    /// </summary>
    [Test]
    public async Task Download_ShouldNonEsporreIlPercorsoDelFile()
    {
        var resp = await Post("""{ "year": 2026, "quarter": 2 }""");

        var tutto = resp.Headers + resp.Content.Headers.ToString() + await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(tutto, Does.Not.Contain("abfss"));
            Assert.That(tutto, Does.Not.Contain("stappioseed"));
            Assert.That(tutto, Does.Not.Contain("appioexport"));
            Assert.That(tutto, Does.Not.Contain("export/sap"));
        });
    }

    /// <summary>
    /// È una lettura: aperta a OPERATOR e ADMIN, 401 senza autenticazione.
    /// </summary>
    [Test]
    public async Task Download_Ruoli_ShouldEssereApertoAOperatorEChiusoAgliAnonimi()
    {
        var operatore = await Post("""{ "year": 2026, "quarter": 2 }""", Ruolo.OPERATOR);
        var anonimo = await Post("""{ "year": 2026, "quarter": 2 }""", ruolo: null);

        Assert.Multiple(() =>
        {
            Assert.That(operatore.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(anonimo.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    /// <summary>
    /// Anno e trimestre arrivano anche come stringhe numeriche (i filtri del frontend sono stringhe):
    /// il binding JSON li accetta.
    /// </summary>
    [Test]
    public async Task Download_AnnoETrimestreComeStringhe_ShouldEssereAccettati()
    {
        var resp = await Post("""{ "year": "2026", "quarter": "2" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
    }

    // --- validazione ---

    /// <summary>
    /// Anno e trimestre sono obbligatori, il trimestre va da 1 a 4: altrimenti 400, senza toccare lo storage.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("""{ "year": 2026 }""")]
    [TestCase("""{ "quarter": 2 }""")]
    [TestCase("""{ "year": 2026, "quarter": 0 }""")]
    [TestCase("""{ "year": 2026, "quarter": 5 }""")]
    [TestCase("""{ "year": 2026, "quarter": -1 }""")]
    [TestCase("""{ "year": 0, "quarter": 2 }""")]
    [TestCase("""{ "year": -2026, "quarter": 2 }""")]
    [TestCase("""{ "year": null, "quarter": null }""")]
    public async Task Download_RichiestaNonValida_ShouldReturn400(string body)
    {
        var resp = await Post(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    // --- 404 e 503 ---

    /// <summary>
    /// Un trimestre senza documenti non ha un file: 404, senza toccare lo storage.
    /// </summary>
    [TestCase(2026, 3)]
    [TestCase(2026, 4)]
    [TestCase(1999, 1)]
    [TestCase(2147483647, 4)]
    public async Task Download_TrimestreSenzaDocumenti_ShouldReturn404(int year, int quarter)
    {
        var resp = await Post($$"""{ "year": {{year}}, "quarter": {{quarter}} }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    /// <summary>
    /// Il trimestre ha documenti ma il file non è sul blob storage: 404.
    /// </summary>
    [Test]
    public async Task Download_FileAssenteSulBlobStorage_ShouldReturn404()
    {
        Storage.Assente = true;

        var resp = await Post("""{ "year": 2026, "quarter": 2 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Storage.Richieste, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Storage non configurato (o configurato per un altro account): 503, senza tentare la lettura.
    /// Un trimestre senza documenti resta 404 anche così: prima si guarda il dato.
    /// </summary>
    [Test]
    public async Task Download_StorageNonConfigurato_ShouldReturn503()
    {
        Storage.Leggibile = false;

        var conDocumenti = await Post("""{ "year": 2026, "quarter": 2 }""");
        var senzaDocumenti = await Post("""{ "year": 2026, "quarter": 3 }""");

        Assert.Multiple(() =>
        {
            Assert.That(conDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(senzaDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(Storage.Richieste, Is.Empty);
        });
    }

    // --- input ostili ---

    /// <summary>
    /// Anno e trimestre sono numeri: un testo (injection compresa) non supera il binding. L'errore
    /// viene appiattito a 500 dal gestore globale, come per gli altri body malformati dell'area.
    /// </summary>
    [TestCase("""{ "year": "2026' OR 1=1 --", "quarter": 2 }""")]
    [TestCase("""{ "year": 2026, "quarter": "2; DROP TABLE appio.messages_final_report" }""")]
    [TestCase("""{ "year": 2026.5, "quarter": 2 }""")]
    [TestCase("""{ "year": 99999999999, "quarter": 2 }""")]
    [TestCase("{ malformato")]
    public async Task Download_ValoriNonNumerici_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    /// <summary>
    /// Il client non può indicare il file: campi in più nel body (percorsi, account) vengono ignorati
    /// e allo storage arriva solo il file_path letto dalla vista.
    /// </summary>
    [Test]
    public async Task Download_PercorsoIndicatoDalClient_ShouldEssereIgnorato()
    {
        var resp = await Post("""
            { "year": 2026, "quarter": 2,
              "filePath": "abfss://altro@altroaccount.dfs.core.windows.net/segreti.csv",
              "file_path": "../../etc/passwd", "path": "https://example.test/x.csv" }
            """);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
    }

    /// <summary>
    /// Un nonce emesso per un altro prodotto viene rifiutato con 419.
    /// </summary>
    [Test]
    public async Task Download_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post("""{ "year": 2026, "quarter": 2 }""", prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
        Assert.That(Storage.Richieste, Is.Empty);
    }
}
