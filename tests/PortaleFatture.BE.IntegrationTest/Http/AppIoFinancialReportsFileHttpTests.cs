using System.IO.Compression;
using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// End-to-end della rotta POST api/appio/financialreports/blob/download (12/11/2026, body con
/// "quarters" dal 16/11/2026): scarica i file dei financial report dei trimestri chiesti — un trimestre
/// il file, più trimestri uno ZIP con un file per trimestre. I percorsi vengono da file_path di
/// [be].[vwAppioFinalReport] e restano nel backend; il blob storage è sostituito da
/// <see cref="FakeAppIoFinancialReportStorageService"/>.
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

    private static async Task<Dictionary<string, string>> VociZip(HttpResponseMessage resp)
    {
        using var zip = new ZipArchive(new MemoryStream(await resp.Content.ReadAsByteArrayAsync()), ZipArchiveMode.Read);
        var voci = new Dictionary<string, string>();
        foreach (var voce in zip.Entries)
        {
            using var lettore = new StreamReader(voce.Open(), Encoding.UTF8);
            voci[voce.FullName] = await lettore.ReadToEndAsync();
        }
        return voci;
    }

    // --- un trimestre: il file ---

    /// <summary>
    /// Un trimestre: si riceve il suo file, non uno ZIP, con il nome costruito dal trimestre.
    /// </summary>
    [Test]
    public async Task Download_UnTrimestre_ShouldRestituireIlFile()
    {
        var resp = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(async () =>
        {
            Assert.That(await resp.Content.ReadAsStringAsync(), Is.EqualTo(FakeAppIoFinancialReportStorageService.Contenuto));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_2.csv"));
            Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
        });
    }

    [TestCase("2026_1", "export/sap/2026/1/appio-final_report-2026-q1.csv")]
    [TestCase("2025_4", "export/sap/2025/4/appio-final_report-2025-q4.csv")]
    public async Task Download_AltriTrimestri_ShouldChiedereIlFileDelTrimestre(string trimestre, string coda)
    {
        var resp = await Post($$"""{ "year": {{trimestre[..4]}}, "quarters": ["{{trimestre}}"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Storage.Richieste.Single(), Does.EndWith(coda));
    }

    /// <summary>
    /// Lo stesso trimestre ripetuto, o con spazi, è un trimestre solo: file singolo, non ZIP.
    /// </summary>
    [Test]
    public async Task Download_TrimestreRipetuto_ShouldContareUnaVolta()
    {
        var resp = await Post("""{ "year": 2026, "quarters": ["2026_2", " 2026_2 "] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
        Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
    }

    // --- più trimestri: lo ZIP ---

    /// <summary>
    /// Più file: uno ZIP con un file per trimestre, ciascuno col proprio nome e contenuto, e il nome
    /// dello ZIP dal primo all'ultimo trimestre (in ordine crescente, qualunque sia quello del body).
    /// </summary>
    [Test]
    public async Task Download_PiuTrimestri_ShouldRestituireUnoZipConUnFilePerTrimestre()
    {
        var resp = await Post("""{ "year": 2026, "quarters": ["2026_2", "2026_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var voci = await VociZip(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/zip"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_1-2026_2.zip"));
            Assert.That(voci.Keys, Is.EqualTo(new[] { "financial-report-appio-2026_1.csv", "financial-report-appio-2026_2.csv" }));
            Assert.That(voci.Values, Is.All.EqualTo(FakeAppIoFinancialReportStorageService.Contenuto));
            Assert.That(Storage.Richieste, Has.Count.EqualTo(2));
        });
    }

    /// <summary>
    /// Anno senza trimestri (lista assente o vuota): si scaricano tutti i trimestri dell'anno che hanno
    /// un file. Nel seed il 2026 ne ha due (ZIP), il 2025 uno solo (il CSV).
    /// </summary>
    [TestCase("""{ "year": 2026 }""")]
    [TestCase("""{ "year": 2026, "quarters": [] }""")]
    [TestCase("""{ "year": "2026", "quarters": null }""")]
    public async Task Download_AnnoSenzaTrimestri_ShouldScaricareTuttiQuelliDisponibili(string body)
    {
        var resp = await Post(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await VociZip(resp)).Keys, Is.EqualTo(new[] { "financial-report-appio-2026_1.csv", "financial-report-appio-2026_2.csv" }));
    }

    [Test]
    public async Task Download_AnnoSenzaTrimestri_UnSoloFile_ShouldRestituireIlCsv()
    {
        var resp = await Post("""{ "year": 2025 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2025_4.csv"));
        });
    }

    /// <summary>
    /// Conta il numero di file disponibili, non quello dei trimestri chiesti: due trimestri, uno senza
    /// documenti, danno il CSV dell'altro e non uno ZIP con un solo file.
    /// </summary>
    [Test]
    public async Task Download_PiuTrimestri_UnoSenzaDocumenti_ShouldRestituireIlCsvDellAltro()
    {
        var resp = await Post("""{ "year": 2026, "quarters": ["2026_1", "2026_3"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_1.csv"));
        });
    }

    /// <summary>
    /// Lo stesso vale per un file in vista ma assente sul blob storage.
    /// </summary>
    [Test]
    public async Task Download_PiuTrimestri_UnFileAssente_ShouldRestituireIlCsvDellAltro()
    {
        Storage.RendiAssente("2026-q1.csv");

        var resp = await Post("""{ "year": 2026, "quarters": ["2026_1", "2026_2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(async () =>
        {
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/csv"));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_2.csv"));
            Assert.That(await resp.Content.ReadAsStringAsync(), Is.EqualTo(FakeAppIoFinancialReportStorageService.Contenuto));
        });
    }

    /// <summary>
    /// Più trimestri, nessuno con un file: 404, non uno ZIP vuoto.
    /// </summary>
    [Test]
    public async Task Download_PiuTrimestri_NessunFile_ShouldReturn404()
    {
        var senzaDocumenti = await Post("""{ "year": 2026, "quarters": ["2026_3", "2026_4"] }""");
        Storage.Assente = true;
        var fileAssenti = await Post("""{ "year": 2026, "quarters": ["2026_1", "2026_2"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(senzaDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(fileAssenti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    // --- percorso e ruoli ---

    [Test]
    public async Task Download_ShouldNonEsporreIlPercorsoDelFile()
    {
        var singolo = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""");
        var zip = await Post("""{ "year": 2026, "quarters": ["2026_1", "2026_2"] }""");

        var tutto = singolo.Headers + singolo.Content.Headers.ToString() + await singolo.Content.ReadAsStringAsync()
            + zip.Headers + zip.Content.Headers + string.Join("|", (await VociZip(zip)).Keys);
        Assert.Multiple(() =>
        {
            Assert.That(tutto, Does.Not.Contain("abfss"));
            Assert.That(tutto, Does.Not.Contain("stappioseed"));
            Assert.That(tutto, Does.Not.Contain("appioexport"));
            Assert.That(tutto, Does.Not.Contain("export/sap"));
        });
    }

    [Test]
    public async Task Download_Ruoli_ShouldEssereApertoAOperatorEChiusoAgliAnonimi()
    {
        var operatore = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""", Ruolo.OPERATOR);
        var anonimo = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""", ruolo: null);

        Assert.Multiple(() =>
        {
            Assert.That(operatore.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(anonimo.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    // --- validazione ---

    /// <summary>
    /// Body assente, senza anno, con trimestri non nel formato 'AAAA_T' o di un altro anno: 400, e lo
    /// storage non viene toccato. Il vecchio campo "quarter" (fino al 16/11/2026) è ignorato.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("""{ "quarters": ["2026_2"] }""")]
    [TestCase("""{ "year": null, "quarters": ["2026_2"] }""")]
    [TestCase("""{ "year": 0, "quarters": ["2026_2"] }""")]
    [TestCase("""{ "year": 26 }""")]
    [TestCase("""{ "quarter": 2 }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2025_4"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_2", "2025_4"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_5"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_0"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026-2"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_2", ""] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_2", null] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_2' OR 1=1 --"] }""")]
    [TestCase("""{ "year": 2026, "quarters": ["2026_2; DROP TABLE appio.messages_final_report"] }""")]
    public async Task Download_RichiestaNonValida_ShouldReturn400(string body)
    {
        var resp = await Post(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    // --- 404 e 503 ---

    [TestCase("2026_3")]
    [TestCase("2026_4")]
    [TestCase("1999_1")]
    public async Task Download_TrimestreSenzaDocumenti_ShouldReturn404(string trimestre)
    {
        var resp = await Post($$"""{ "year": {{trimestre[..4]}}, "quarters": ["{{trimestre}}"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    [Test]
    public async Task Download_FileAssenteSulBlobStorage_ShouldReturn404()
    {
        Storage.Assente = true;

        var resp = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Storage.Richieste, Has.Count.EqualTo(1));
    }

    /// <summary>
    /// Storage non configurato: 503 se c'è almeno un file da leggere (anche con più trimestri), 404 se
    /// nessun trimestre chiesto ha documenti.
    /// </summary>
    [Test]
    public async Task Download_StorageNonConfigurato_ShouldReturn503()
    {
        Storage.Leggibile = false;

        var conDocumenti = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""");
        var piuTrimestri = await Post("""{ "year": 2026, "quarters": ["2026_1", "2026_2"] }""");
        var senzaDocumenti = await Post("""{ "year": 2026, "quarters": ["2026_3"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(conDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(piuTrimestri.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(senzaDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(Storage.Richieste, Is.Empty);
        });
    }

    // --- input ostili ---

    /// <summary>
    /// Body che non si legge come { year: int, quarters: string[] }: il binding fallisce (500, comportamento
    /// della pipeline comune, caratterizzato e non corretto qui) e lo storage non viene toccato.
    /// </summary>
    [TestCase("""{ "year": 2026, "quarters": "2026_2" }""")]
    [TestCase("""{ "year": 2026, "quarters": [2026] }""")]
    [TestCase("{ malformato")]
    public async Task Download_BodyNonLeggibile_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(Storage.Richieste, Is.Empty);
    }

    [Test]
    public async Task Download_PercorsoIndicatoDalClient_ShouldEssereIgnorato()
    {
        var resp = await Post("""
            { "year": 2026, "quarters": ["2026_2"],
              "filePath": "abfss://altro@altroaccount.dfs.core.windows.net/segreti.csv",
              "file_path": "../../etc/passwd", "path": "https://example.test/x.csv" }
            """);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Storage.Richieste, Is.EqualTo(new[] { Path2026Q2 }));
    }

    [Test]
    public async Task Download_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post("""{ "year": 2026, "quarters": ["2026_2"] }""", prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
        Assert.That(Storage.Richieste, Is.Empty);
    }
}
