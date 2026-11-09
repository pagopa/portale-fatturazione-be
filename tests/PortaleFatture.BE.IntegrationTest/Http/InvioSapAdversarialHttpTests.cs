using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Passata adversarial sulle sette rotte dell'Invio a SAP: filtri ostili, valori estremi, body sbagliati,
/// injection, liste enormi. Regola di giudizio: un input assurdo e' un errore del client (400/404/409) o un
/// risultato vuoto, mai un 500 con stack trace, e non scrive nulla (be-invio-sap.md §13.3).
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapAdversarialHttpTests
{
    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        InvioSapTestData.SkipSeOggettiAssenti();
        _factory = new ApiTestFactory();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        InvioSapTestData.Pulisci();
        _factory?.Dispose();
    }

    [SetUp]
    public void Reset()
    {
        FakeSynapseService.Instance.Reset();
        InvioSapTestData.Pulisci();
    }

    private static async Task SenzaStackTrace(HttpResponseMessage resp)
    {
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.That(testo, Does.Not.Contain("   at ").And.Not.Contain("SqlException").And.Not.Contain("Exception:"));
    }

    // =============================================================================================
    // Griglia e download: filtri
    // =============================================================================================

    [TestCase("{}", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: anno assente)")]
    [TestCase("""{ "anno": null }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: anno null)")]
    [TestCase("""{ "anno": 1999 }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: anno sotto il minimo)")]
    [TestCase("""{ "anno": 2099 }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: anno lontano)")]
    [TestCase("""{ "anno": 2003, "mesi": [13] }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: mese 13)")]
    [TestCase("""{ "anno": 2003, "mesi": [0] }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: mese 0)")]
    [TestCase("""{ "anno": 2003, "mesi": [5, 5] }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: mesi ripetuti)")]
    [TestCase("""{ "anno": 2003, "statoInvio": "REINVIATE" }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: stato non ammesso)")]
    [TestCase("""{ "anno": 2003, "statoInvio": "INVIATA" }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: stato singolare)")]
    [TestCase("""{ "anno": 2003, "statoInvio": "'; DROP TABLE pfw.RegistroInvioSAP; --" }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: stato ostile)")]
    [TestCase("""{ "anno": 2003, "tipologieFattura": [""] }""", TestName = "Griglia_FiltriNonValidi_ShouldRispondere400(Griglia400: tipologia vuota)")]
    public async Task Griglia_FiltriNonValidi_ShouldRispondere400(string body)
    {
        foreach (var rotta in new[] { InvioSapTestData.RottaGriglia, InvioSapTestData.RottaDownload })
        {
            var resp = await Post(_factory, rotta, body);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), rotta);
            await SenzaStackTrace(resp);
        }
    }

    [TestCase("""{ "anno": 2003, "tipologieFattura": ["' OR 1=1 --"] }""", TestName = "Griglia_TipologiaOstile_ShouldEssereSoloUnValore(Griglia404: injection nella tipologia)")]
    [TestCase("""{ "anno": 2003, "tipologieFattura": ["PRIMO SALDO' --"] }""", TestName = "Griglia_TipologiaOstile_ShouldEssereSoloUnValore(Griglia404: apice nella tipologia)")]
    public async Task Griglia_TipologiaOstile_ShouldEssereSoloUnValore(string body)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Il parametro Dapper la tratta come testo: nessun rigo.");
    }

    [Test]
    public async Task Griglia_TipologieInMassa_ShouldNonRompere()
    {
        var tipologie = Enumerable.Range(0, 1000).Select(i => $"T{i}").Append("PRIMO SALDO").ToArray();
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, JsonSerializer.Serialize(new { anno = 2003, tipologieFattura = tipologie }, Web));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Caratterizzazione di un comportamento di TUTTA l'API, non dell'Invio a SAP: con
    /// <c>ThrowOnBadRequest = true</c> (ModuleExtensions) l'errore di binding del body diventa un 500 nel gestore
    /// globale, come in AppIoContrattiAdversarialHttpTests e PSPAdversarialHttpTests. Portarlo a 400 cambia il
    /// contratto di tutte le rotte: va deciso col FE, non qui. Si verifica almeno che lo stack trace non esca.
    /// </summary>
    [TestCase("""{ "anno": "duemilatre" }""", TestName = "Griglia_BodyMalformato_ShouldRispondere500_Caratterizzazione(Griglia500: anno testo, caratterizzazione)")]
    [TestCase("""{ "anno": 2003, "mesi": 5 }""", TestName = "Griglia_BodyMalformato_ShouldRispondere500_Caratterizzazione(Griglia500: mesi scalare, caratterizzazione)")]
    [TestCase("non-json", TestName = "Griglia_BodyMalformato_ShouldRispondere500_Caratterizzazione(Griglia500: non JSON, caratterizzazione)")]
    public async Task Griglia_BodyMalformato_ShouldRispondere500_Caratterizzazione(string body)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        await SenzaStackTrace(resp);
    }

    [Test]
    public async Task DownloadReinviate_SenzaAnno_ShouldEssere400()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, "{}");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // =============================================================================================
    // Dettaglio
    // =============================================================================================

    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "ragioneSociale": "%" }""", 0, TestName = "Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(Dettaglio: % letterale)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "ragioneSociale": "_" }""", 0, TestName = "Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(Dettaglio: _ letterale)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "ragioneSociale": "[E]" }""", 0, TestName = "Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(Dettaglio: [ letterale)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "ragioneSociale": "' OR 1=1 --" }""", 0, TestName = "Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(Dettaglio: injection)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "numeroFattura": "%" }""", 0, TestName = "Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(Dettaglio: numero %)")]
    public async Task Dettaglio_CaratteriSpecialiDelLike_ShouldEssereLetterali(string body, int attese)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDettaglio, body);
        Assert.That(resp.StatusCode, Is.EqualTo(attese == 0 ? HttpStatusCode.NotFound : HttpStatusCode.OK),
            "Escapati: '%' non deve allargare il filtro a tutte le fatture.");
    }

    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "page": 0, "pageSize": 10 }""", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: page 0)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "page": -1, "pageSize": 10 }""", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: page negativa)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "page": 1, "pageSize": 2147483647 }""", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: pageSize int.MaxValue)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO" }""", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: stato assente)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "TUTTE" }""", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: TUTTE non e' un rigo)")]
    [TestCase("{}", TestName = "Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(Dettaglio400: body vuoto)")]
    public async Task Dettaglio_ParametriOstili_ShouldEssere4xx_MaiUn500(string body)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDettaglio, body);
        Assert.That((int)resp.StatusCode, Is.InRange(400, 499));
        await SenzaStackTrace(resp);
    }

    /// <summary>
    /// Page vicino a int.MaxValue: (Page - 1) * PageSize in int andava in overflow (500). Con l'offset in bigint
    /// e' una pagina oltre la fine: 200, conteggio vero e lista vuota, come ogni pagina oltre l'ultima.
    /// </summary>
    [Test]
    public async Task Dettaglio_PageEnorme_ShouldEssere200_PaginaVuota_SenzaOverflow()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "page": 2147483647, "pageSize": 500 }""");
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.GreaterThan(0));
            Assert.That(corpo.GetProperty("fatture").GetArrayLength(), Is.Zero);
        });
    }

    // =============================================================================================
    // Genera
    // =============================================================================================

    /// <summary>Come Griglia_BodyMalformato_ShouldRispondere500_Caratterizzazione: comportamento di tutta l'API.</summary>
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "idFatture": ["abc"], "nota": "x" }""", TestName = "Genera_BodyMalformato_ShouldRispondere500_SenzaScritture_Caratterizzazione(Genera500: id non numerico, caratterizzazione)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "idFatture": 9502, "nota": "x" }""", TestName = "Genera_BodyMalformato_ShouldRispondere500_SenzaScritture_Caratterizzazione(Genera500: idFatture scalare, caratterizzazione)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "idFatture": [99999999999999999999], "nota": "x" }""", TestName = "Genera_BodyMalformato_ShouldRispondere500_SenzaScritture_Caratterizzazione(Genera500: id oltre long, caratterizzazione)")]
    [TestCase("non-json", TestName = "Genera_BodyMalformato_ShouldRispondere500_SenzaScritture_Caratterizzazione(Genera500: non JSON, caratterizzazione)")]
    public async Task Genera_BodyMalformato_ShouldRispondere500_SenzaScritture_Caratterizzazione(string body)
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        await SenzaStackTrace(resp);
        Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Il binding fallisce prima dell'handler: nessun batch.");
    }

    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "idFatture": [], "nota": "x", "numeroDocumentoStorno": "NC" }""", "Selezionare", TestName = "Genera_BodyNonValido_ShouldRispondere400_SenzaScritture(Genera400: idFatture vuota non e' assente)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "idFatture": [-1, 0], "nota": "x", "numeroDocumentoStorno": "NC" }""", "non valido", TestName = "Genera_BodyNonValido_ShouldRispondere400_SenzaScritture(Genera400: id negativi)")]
    [TestCase("""{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO" }""", "nota", TestName = "Genera_BodyNonValido_ShouldRispondere400_SenzaScritture(Genera400: nota assente)")]
    [TestCase("{}", "Anno", TestName = "Genera_BodyNonValido_ShouldRispondere400_SenzaScritture(Genera400: body vuoto)")]
    public async Task Genera_BodyNonValido_ShouldRispondere400_SenzaScritture(string body, string frammento)
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, body);
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain(frammento).IgnoreCase);
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
        });
    }

    [Test]
    public async Task Genera_NotaDa100KB_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(2003, 2, "PRIMO SALDO", new string('n', 100_000)));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Genera_DiecimilaId_ShouldEssere400_NonUnaQueryEnorme()
    {
        InvioSapTestData.RilasciaLockComeData();
        var ids = Enumerable.Range(1, 10_000).Select(i => (long)i).ToArray();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(2003, 1, "PRIMO SALDO", "x", ids, "NC"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Genera_IdEnorme_ShouldEssere404()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(2003, 1, "PRIMO SALDO", "x", [long.MaxValue], "NC"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Genera_TipologiaOstile_ShouldEssere404_SenzaScritture()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(2003, 2, "PRIMO SALDO' OR 1=1 --", "x"));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    [Test]
    public async Task Genera_StornoConApiciEUnicode_ShouldEssereScrittoTaleQuale()
    {
        InvioSapTestData.RilasciaLockComeData();
        const string storno = "NC-'2003'-ü-🚀";
        var resp = await Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(2003, 1, "PRIMO SALDO", "x", [9502], storno));
        var batchId = (await Json(resp)).GetProperty("batchId").GetInt32();
        Assert.That(InvioSapTestData.Scalar($"SELECT NumeroDocStorno FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)), Is.EqualTo(storno));
    }

    [Test]
    public async Task RotteRimosse_ShouldEssere404_SenzaStackTrace()
    {
        foreach (var rotta in new[] { "/api/fatture/invio-sap/generazioni/9001", "/api/fatture/invio-sap/generazioni/corrente" })
        {
            var resp = await Get(_factory, rotta);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), rotta);
            await SenzaStackTrace(resp);
        }
    }
}
