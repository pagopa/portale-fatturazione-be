using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte api/appio/financialreports* (modello dati dell'11/11/2026, PF-1016): filtri Anno, Trimestre
/// e Codice Fiscale, griglia (una riga per contratto e trimestre), download "Documenti Contabili",
/// dettaglio e suo download.
///
/// Seed (tests/Data/appio.sql): 2026_2 → OB1/ENTE-1 (APPIO-FR-001 con due righe da 10,
/// APPIO-FR-002 da 5: ImportoTotale 25) e OB2/ENTE-2 (APPIO-FR-003, 2); 2026_1 → OB1 (APPIO-FR-004, 3);
/// 2025_4 → OB4/ENTE-4 (APPIO-FR-005, 1); OB9 orfano, escluso dalle viste.
/// </summary>
[Category("AppIO")]
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
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReport");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportDetails");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportFiltroAnno");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportFiltroCodFiscaleEnte");
    }

    private async Task<HttpResponseMessage> Post(string rotta, string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    private async Task<HttpResponseMessage> Get(string rotta, string? ruolo = Ruolo.ADMIN)
        => await _factory.CreateClientAs(ruolo).GetAsync(_factory.WithNonce(rotta));

    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    private static List<string?> Valori(JsonElement array, string proprieta)
        => array.EnumerateArray().Select(x => x.GetProperty(proprieta).GetString()).ToList();

    private static async Task<string> TestoExcel(HttpResponseMessage resp)
    {
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        return string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));
    }

    // --- filtri ---

    /// <summary>
    /// Il filtro "Anno" restituisce gli anni come stringhe, dal più recente, anche all'OPERATOR.
    /// </summary>
    [Test]
    public async Task Years_ShouldRestituireGliAnniDalPiuRecente()
    {
        var resp = await Get($"{Base}/years", Ruolo.OPERATOR);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Json(resp)).EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "2026", "2025" }));
    }

    /// <summary>
    /// Il filtro "Trimestre" restituisce i trimestri dell'anno nel formato 'AAAA_T', con etichetta "Qn".
    /// </summary>
    [Test]
    public async Task Quarters_PerAnno_ShouldRestituireITrimestriInOrdine()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(json, "value"), Is.EqualTo(new[] { "2026_1", "2026_2" }));
            Assert.That(Valori(json, "quarter"), Is.EqualTo(new[] { "Q1", "Q2" }));
        });
    }

    /// <summary>
    /// Un anno senza documenti risponde 404.
    /// </summary>
    [Test]
    public async Task Quarters_AnnoSenzaDati_ShouldReturn404()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "1999" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Il filtro "Codice Fiscale" restituisce i codici fiscali distinti, in ordine.
    /// </summary>
    [Test]
    public async Task CodFiscale_ShouldRestituireICodiciDistinti()
    {
        var resp = await Get($"{Base}/codfiscale");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Json(resp)).EnumerateArray().Select(x => x.GetString()),
            Is.EqualTo(new[] { "00000000001", "00000000002", "00000000003", "00000000004" }));
    }

    // --- griglia ---

    /// <summary>
    /// Senza filtri la griglia mostra il trimestre più recente con una riga per contratto: OB1 ha due
    /// documenti nel trimestre ma è una sola riga, con ImportoTotale 25 e tre posizioni.
    /// </summary>
    [Test]
    public async Task Griglia_SenzaFiltri_ShouldMostrareUnaRigaPerContrattoNelTrimestrePiuRecente()
    {
        var resp = await Post(Base, "{}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        var righe = json.GetProperty("financialReports");
        var ob1 = righe[0];
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(Valori(righe, "key"), Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB2|2026_2" }));
            Assert.That(ob1.GetProperty("institutionId").GetString(), Is.EqualTo("APPIO-ENTE-1"));
            Assert.That(ob1.GetProperty("codiceFiscale").GetString(), Is.EqualTo("00000000001"));
            Assert.That(ob1.GetProperty("year").GetInt32(), Is.EqualTo(2026));
            Assert.That(ob1.GetProperty("quarter").GetInt32(), Is.EqualTo(2));
            Assert.That(ob1.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(25m));
            Assert.That(Valori(ob1.GetProperty("posizioni"), "numero"),
                Is.EqualTo(new[] { "APPIO-FR-001", "APPIO-FR-001", "APPIO-FR-002" }));
        });
    }

    /// <summary>
    /// Con l'anno si vedono tutti i trimestri dell'anno, dal più recente per ogni contratto.
    /// </summary>
    [Test]
    public async Task Griglia_ConAnno_ShouldMostrareTuttiITrimestriDellAnno()
    {
        var resp = await Post(Base, """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori((await Json(resp)).GetProperty("financialReports"), "key"),
            Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB1|2026_1", "APPIO-OB2|2026_2" }));
    }

    /// <summary>
    /// I trimestri prevalgono sull'anno.
    /// </summary>
    [Test]
    public async Task Griglia_TrimestriEAnno_ShouldPrevalereITrimestri()
    {
        var resp = await Post(Base, """{ "quarters": ["2025_4"], "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori((await Json(resp)).GetProperty("financialReports"), "key"), Is.EqualTo(new[] { "APPIO-OB4|2025_4" }));
    }

    /// <summary>
    /// Ente (dal filtro "Nome Ente") e codice fiscale filtrano la griglia, in AND con il periodo.
    /// </summary>
    [Test]
    public async Task Griglia_EnteECodiceFiscale_ShouldFiltrare()
    {
        var perEnte = Valori((await Json(await Post(Base, """{ "year": "2026", "institutionIds": ["APPIO-ENTE-1"] }"""))).GetProperty("financialReports"), "key");
        var perCodice = Valori((await Json(await Post(Base, """{ "year": "2026", "codiciFiscali": ["00000000002"] }"""))).GetProperty("financialReports"), "key");

        Assert.Multiple(() =>
        {
            Assert.That(perEnte, Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB1|2026_1" }));
            Assert.That(perCodice, Is.EqualTo(new[] { "APPIO-OB2|2026_2" }));
        });
    }

    /// <summary>
    /// Un filtro che non trova documenti risponde 404.
    /// </summary>
    [Test]
    public async Task Griglia_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post(Base, """{ "quarters": ["1999_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Senza autenticazione la griglia risponde 401.
    /// </summary>
    [Test]
    public async Task Griglia_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(Base, "{}", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- download "Documenti Contabili" ---

    /// <summary>
    /// Il download applica lo stesso filtro della griglia: una riga per riga di documento.
    /// </summary>
    [Test]
    public async Task Document_ShouldEsportareLeRigheDelFiltro()
    {
        var resp = await Post($"{Base}/document", """{ "year": "2026", "institutionIds": ["APPIO-ENTE-1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        var testo = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("APPIO-FR-001"));
            Assert.That(testo, Does.Contain("APPIO-FR-004"));
            Assert.That(testo, Does.Not.Contain("APPIO-FR-003"), "FR-003 è di ENTE-2");
        });
    }

    // --- dettaglio ---

    /// <summary>
    /// Il dettaglio di una riga della griglia restituisce anagrafica e righe dei documenti del trimestre.
    /// </summary>
    [Test]
    public async Task Detail_ShouldRestituireAnagraficaERighe()
    {
        var resp = await Post($"{Base}/detail", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_2" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        var posizioni = json.GetProperty("posizioni");
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("name").GetString(), Is.EqualTo("Comune Alfa AppIO Test"));
            Assert.That(json.GetProperty("partitaIva").GetString(), Is.EqualTo("00000000001"));
            Assert.That(json.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(25m));
            Assert.That(json.GetProperty("year").GetInt32(), Is.EqualTo(2026));
            Assert.That(Valori(posizioni, "numero"), Is.EqualTo(new[] { "APPIO-FR-001", "APPIO-FR-001", "APPIO-FR-002" }));
            Assert.That(Valori(posizioni, "tipoDoc"), Is.EqualTo(new[] { "TD01", "TD01", "TD04" }));
            Assert.That(posizioni[0].GetProperty("dataReport").GetDateTime(), Is.EqualTo(new DateTime(2026, 7, 15)));
        });
    }

    /// <summary>
    /// Una data in formato errato a DB arriva come null, senza far fallire la rotta.
    /// </summary>
    [Test]
    public async Task Detail_DataNonConvertibile_ShouldArrivareNull()
    {
        var resp = await Post($"{Base}/detail", """{ "contractId": "APPIO-OB4", "yearQuarter": "2025_4" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var riga = (await Json(resp)).GetProperty("posizioni")[0];
        Assert.Multiple(() =>
        {
            Assert.That(riga.GetProperty("dataReport").GetDateTime(), Is.EqualTo(new DateTime(2026, 1, 15)));
            Assert.That(riga.GetProperty("dataRiferimento").ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    /// <summary>
    /// Contratto e trimestre sono obbligatori: senza uno dei due la rotta risponde 400.
    /// </summary>
    [TestCase("{}")]
    [TestCase("""{ "contractId": "APPIO-OB1" }""")]
    [TestCase("""{ "yearQuarter": "2026_2" }""")]
    [TestCase("")]
    public async Task DetailEDownload_SenzaContrattoOTrimestre_ShouldReturn400(string body)
    {
        var detail = await Post($"{Base}/detail", body);
        var download = await Post($"{Base}/detail/download", body);

        Assert.Multiple(() =>
        {
            Assert.That(detail.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
    }

    /// <summary>
    /// Un contratto senza documenti nel trimestre risponde 404, sul dettaglio e sul download.
    /// </summary>
    [Test]
    public async Task DetailEDownload_DocumentoInesistente_ShouldReturn404()
    {
        const string body = """{ "contractId": "APPIO-OB3", "yearQuarter": "2026_2" }""";
        var detail = await Post($"{Base}/detail", body);
        var download = await Post($"{Base}/detail/download", body);

        Assert.Multiple(() =>
        {
            Assert.That(detail.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// Il download del dettaglio è un Excel con le righe del solo contratto e trimestre richiesti.
    /// </summary>
    [Test]
    public async Task DetailDownload_ShouldEsportareSoloIlDocumentoRichiesto()
    {
        var resp = await Post($"{Base}/detail/download", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_1" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var testo = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("APPIO-FR-004"));
            Assert.That(testo, Does.Not.Contain("APPIO-FR-001"), "FR-001 è del 2026_2");
        });
    }
}
