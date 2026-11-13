using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili e casi limite sui casi del testbook APP IO (PF-550_TESTBOOK_APP-IO): cosa succede
/// quando il filtro di un caso riceve un valore che la pagina normalmente non manda. Ogni test porta
/// nel nome il caso del testbook a cui si riferisce. I test con suffisso <c>_Caratterizzazione</c>
/// fissano il comportamento attuale dove è discutibile: se cambia, il test va aggiornato insieme
/// alla decisione.
///
/// Stessi dati di <see cref="AppIoTestbookHttpTests"/> (seed più i due contratti di
/// <see cref="AppIoTestbookDati"/>).
/// </summary>
[Category("AppIO")]
[Category("TestbookAppIO")]
public class AppIoTestbookAdversarialHttpTests
{
    private const string Contratti = "/api/appio/contracts";
    private const string Documenti = "/api/appio/financialreports";
    private const string Pagina = "&page=1&pageSize=50";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _factory = new ApiTestFactory();
        AppIoTestbookDati.Inserisci();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        AppIoTestbookDati.Rimuovi();
        _factory?.Dispose();
    }

    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContracts");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReport");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioFinalReportDetails");
        FakeAppIoFinancialReportStorageService.Instance.Reset();
    }

    private async Task<HttpResponseMessage> Post(string rotta, string body = "{}", string query = "")
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).PostAsync(_factory.WithNonce(rotta) + query, content);
        TestContext.Out.WriteLine($"POST {rotta}{query} {(body.Length > 200 ? body[..200] + "…" : body)} -> {(int)resp.StatusCode}");
        return resp;
    }

    private static string J(object o) => JsonSerializer.Serialize(o);

    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    private static List<string?> Valori(JsonElement array, string proprieta)
        => array.EnumerateArray().Select(x => x.GetProperty(proprieta).GetString()).ToList();

    private async Task<List<string?>> ContrattiTrovati(string body)
    {
        var resp = await Post(Contratti, body, Pagina);
        return resp.StatusCode == HttpStatusCode.OK ? Valori((await Json(resp)).GetProperty("contratti"), "contractId") : [];
    }

    private async Task<List<string?>> ChiaviDocumenti(string body)
    {
        var resp = await Post(Documenti, body);
        return resp.StatusCode == HttpStatusCode.OK ? Valori((await Json(resp)).GetProperty("financialReports"), "key") : [];
    }

    private static async Task<int> RigheDelSeed()
    {
        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(
            "SELECT (SELECT COUNT(*) FROM appio.messages_final_report WHERE contract_id LIKE 'APPIO-OB%') * 100 + (SELECT COUNT(*) FROM appio.contracts WHERE onboardingtokenid LIKE 'APPIO-%')", conn);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    // =========================================================== T03–T05, T08: Tipo Profilo

    /// <summary>
    /// T03–T05 — Un profilo che non esiste, vuoto o con un tentativo di injection non trova nulla:
    /// resta un valore, non allarga il filtro.
    /// </summary>
    [TestCase("XYZ")]
    [TestCase("")]
    [TestCase("PA' OR '1'='1")]
    [TestCase("PA'; DROP TABLE appio.contracts; --")]
    [TestCase("%")]
    [TestCase("P_")]
    public async Task T03_T05_TipoProfiloNonValido_ShouldNonTrovareNulla(string tipo)
    {
        var resp = await Post(Contratti, J(new { tipiEnte = new[] { tipo } }), Pagina);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await RigheDelSeed(), Is.EqualTo(707), "tabelle intatte: 7 righe di documento, 7 contratti");
    }

    /// <summary>
    /// T03–T05 — Selezionare tutti i profili non equivale a nessun filtro: resta fuori il contratto
    /// senza profilo (OB5), che compare solo senza filtro. Un profilo ripetuto non duplica le righe.
    /// </summary>
    [Test]
    public async Task T03_T05_TuttiIProfili_ShouldEscludereIContrattiSenzaProfilo()
    {
        var tutti = await ContrattiTrovati("""{ "tipiEnte": ["PA", "GSP", "PSP"] }""");
        var ripetuto = await ContrattiTrovati("""{ "tipiEnte": ["PSP", "PSP", "PSP"] }""");
        var senzaFiltro = await ContrattiTrovati("{}");

        Assert.Multiple(() =>
        {
            Assert.That(tutti, Has.Count.EqualTo(6).And.None.EqualTo("APPIO-OB5"));
            Assert.That(senzaFiltro, Has.Count.EqualTo(7).And.Contains("APPIO-OB5"));
            Assert.That(ripetuto, Is.EqualTo(new[] { "APPIO-TB2" }));
        });
    }

    /// <summary>
    /// T03–T05 — Il confronto sul profilo non distingue maiuscole e minuscole (collation del
    /// database): "pa" trova gli enti PA. Uno spazio in coda viene ignorato dal confronto SQL.
    /// </summary>
    [TestCase("pa")]
    [TestCase("Pa")]
    [TestCase("PA ")]
    public async Task T03_TipoProfiloConAltraGrafia_ShouldTrovareGliStessiEnti_Caratterizzazione(string tipo)
    {
        var trovati = await ContrattiTrovati(J(new { tipiEnte = new[] { tipo } }));

        Assert.That(trovati, Is.EquivalentTo(new[] { "APPIO-OB1", "APPIO-OB2", "APPIO-OB3" }));
    }

    /// <summary>
    /// T08 — Liste vuote o nulle valgono come filtro assente; il filtro sull'ente da solo non è
    /// limitato dal profilo.
    /// </summary>
    [TestCase("""{ "tipiEnte": [], "institutionIds": [] }""")]
    [TestCase("""{ "tipiEnte": null, "institutionIds": null }""")]
    [TestCase("")]
    [TestCase("null")]
    public async Task T08_FiltriVuoti_ShouldValereComeNessunFiltro(string body)
    {
        var trovati = await ContrattiTrovati(body);

        Assert.That(trovati, Has.Count.EqualTo(AppIoTestbookDati.ContrattiTotali));
    }

    // ================================================================ T06, T07, T09: Ente

    /// <summary>
    /// T06 — La ricerca non distingue maiuscole e minuscole e trova anche una parte del nome.
    /// </summary>
    [TestCase("ACQUEDOTTO")]
    [TestCase("AcQuEdOtTo")]
    [TestCase("quedott")]
    [TestCase("TESTBOOK S.P.A.")]
    public async Task T06_RicercaConAltraGrafiaOParziale_ShouldTrovareLEnte(string testo)
    {
        var resp = await Post($"{Contratti}/name", J(new { name = testo }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori(await Json(resp), "name"), Is.EqualTo(new[] { AppIoTestbookDati.NomeAcquedotto }));
    }

    /// <summary>
    /// T06 — Injection e testi che non compaiono in nessun nome non trovano nulla, e le tabelle restano
    /// intatte. Gli spazi attorno al testo non vengono tolti: " acquedotto " non trova l'ente.
    /// </summary>
    [TestCase("acquedotto' OR '1'='1")]
    [TestCase("'; DROP TABLE appio.contracts; --")]
    [TestCase(" acquedotto ")]
    [TestCase("acquedotto  testbook")]
    public async Task T06_RicercaOstile_ShouldNonTrovareNulla(string testo)
    {
        var resp = await Post($"{Contratti}/name", J(new { name = testo }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await RigheDelSeed(), Is.EqualTo(707));
    }

    /// <summary>
    /// T06 — Il testo finisce in un LIKE senza escape: '%' e '_' fanno da jolly. "%" propone tutti gli
    /// enti con un nome, "acque%book" trova l'acquedotto. Stesso comportamento della ricerca PSP.
    /// </summary>
    [TestCase("%", 6)]
    [TestCase("_", 6)]
    [TestCase("acque%book", 1)]
    [TestCase("acquedott_ testbook", 1)]
    public async Task T06_RicercaConJolly_ShouldAllargareLaRicerca_Caratterizzazione(string testo, int attesi)
    {
        var resp = await Post($"{Contratti}/name", J(new { name = testo }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Json(resp)).GetArrayLength(), Is.EqualTo(attesi));
    }

    /// <summary>
    /// T06 — Un testo molto lungo non manda in errore la ricerca.
    /// </summary>
    [TestCase(1000)]
    [TestCase(4000)]
    public async Task T06_RicercaMoltoLunga_ShouldNonTrovareNulla(int lunghezza)
    {
        var resp = await Post($"{Contratti}/name", J(new { name = new string('a', lunghezza) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T06 — L'autocomplete propone un ente una volta sola anche se ha lo stesso nome di un altro:
    /// "Comune Alfa" ha due enti omonimi (PA e GSP) e compaiono entrambi, distinti per id.
    /// </summary>
    [Test]
    public async Task T06_EntiOmonimi_ShouldRestareDistintiPerId()
    {
        var voci = await Json(await Post($"{Contratti}/name", """{ "name": "Comune Alfa" }"""));

        Assert.Multiple(() =>
        {
            Assert.That(Valori(voci, "name"), Is.EqualTo(new[] { "Comune Alfa AppIO Test", "Comune Alfa AppIO Test" }));
            Assert.That(Valori(voci, "institutionId"), Is.EquivalentTo(new[] { "APPIO-ENTE-1", "APPIO-ENTE-4" }));
        });
    }

    /// <summary>
    /// T07 — L'id dell'ente è un valore: ripetuto non duplica la riga, mescolato a un id inesistente
    /// o a un'injection trova solo l'ente vero.
    /// </summary>
    [Test]
    public async Task T07_IdEnteRipetutoOMescolato_ShouldMostrareSoloLEnteVero()
    {
        var ripetuto = await ContrattiTrovati(J(new { institutionIds = new[] { AppIoTestbookDati.EnteAcquedotto, AppIoTestbookDati.EnteAcquedotto } }));
        var mescolato = await ContrattiTrovati(J(new { institutionIds = new[] { AppIoTestbookDati.EnteAcquedotto, "ENTE-INESISTENTE", "' OR 1=1 --" } }));

        Assert.Multiple(() =>
        {
            Assert.That(ripetuto, Is.EqualTo(new[] { AppIoTestbookDati.ContrattoAcquedotto }));
            Assert.That(mescolato, Is.EqualTo(new[] { AppIoTestbookDati.ContrattoAcquedotto }));
        });
    }

    /// <summary>
    /// T07 — Il nome dell'ente al posto dell'id non seleziona nulla: la griglia filtra per id.
    /// </summary>
    [Test]
    public async Task T07_NomeAlPostoDellId_ShouldNonTrovareNulla()
    {
        var resp = await Post(Contratti, J(new { institutionIds = new[] { AppIoTestbookDati.NomeAcquedotto } }), Pagina);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // ============================================================== T01, T10: paginazione

    /// <summary>
    /// T01 — Una pagina oltre l'ultima risponde 200 con count giusto e nessuna riga: il 404 è solo
    /// per il filtro che non trova nulla.
    /// </summary>
    [Test]
    public async Task T01_PaginaOltreLUltima_ShouldRestituireCountSenzaRighe_Caratterizzazione()
    {
        var resp = await Post(Contratti, "{}", "&page=99&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(AppIoTestbookDati.ContrattiTotali));
            Assert.That(json.GetProperty("contratti").GetArrayLength(), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// T10 — Il download ignora la paginazione anche se viene passata: esporta sempre tutto il filtro.
    /// </summary>
    [Test]
    public async Task T10_DownloadConPaginazione_ShouldEsportareComunqueTutto()
    {
        var conPagina = await Post($"{Contratti}/download", "{}", "&page=1&pageSize=1");

        Assert.That(conPagina.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var zip = new System.IO.Compression.ZipArchive(await conPagina.Content.ReadAsStreamAsync());
        var foglio = string.Concat(zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/")).Select(e => new StreamReader(e.Open()).ReadToEnd()));
        var righeDati = System.Text.RegularExpressions.Regex.Matches(foglio, @"<(?:\w+:)?row\b").Count - 1;
        Assert.That(righeDati, Is.EqualTo(AppIoTestbookDati.ContrattiTotali));
    }

    // ===================================================== T14–T19: filtri dei documenti

    /// <summary>
    /// T14 — L'anno come numero JSON invece che come stringa non supera il binding: 500, come per gli
    /// altri body malformati dell'area. La pagina deve mandarlo come stringa.
    /// </summary>
    [Test]
    public async Task T14_AnnoComeNumero_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post(Documenti, """{ "year": 2026 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// T14 — Un anno senza documenti, futuro o non numerico non trova nulla, senza errori.
    /// </summary>
    [TestCase("2024")]
    [TestCase("2099")]
    [TestCase("anno")]
    [TestCase("2026' OR '1'='1")]
    [TestCase("-2026")]
    public async Task T14_AnnoSenzaDocumenti_ShouldReturn404(string anno)
    {
        var resp = await Post(Documenti, J(new { year = anno }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T15 — I trimestri proposti sono solo quelli con documenti: un anno senza documenti dà 404, e
    /// senza anno arrivano i trimestri di tutti gli anni.
    /// </summary>
    [Test]
    public async Task T15_TrimestriDiAnnoSenzaDocumentiOSenzaAnno()
    {
        var senzaDocumenti = await Post($"{Documenti}/quarters", """{ "year": "2024" }""");
        var tutti = await Post($"{Documenti}/quarters");

        Assert.That(senzaDocumenti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(Valori(await Json(tutti), "value"), Is.EquivalentTo(new[] { "2026_2", "2026_1", "2025_4" }));
    }

    /// <summary>
    /// T16 — Il trimestre va mandato come valore 'AAAA_T' della voce del filtro: l'etichetta "Q1", un
    /// formato diverso o un trimestre inesistente non trovano nulla.
    /// </summary>
    [TestCase("Q1")]
    [TestCase("1")]
    [TestCase("2026-1")]
    [TestCase("2026_5")]
    [TestCase(" 2026_1")]
    [TestCase("2026_1' OR '1'='1")]
    public async Task T16_TrimestreInFormatoErrato_ShouldReturn404(string trimestre)
    {
        var resp = await Post(Documenti, J(new { year = "2026", quarters = new[] { trimestre } }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T16 — Uno spazio in coda al trimestre viene ignorato dal confronto SQL: "2026_1 " trova i
    /// documenti del 2026_1 (uno spazio in testa invece no).
    /// </summary>
    [Test]
    public async Task T16_TrimestreConSpazioInCoda_ShouldTrovareLoStessoTrimestre_Caratterizzazione()
    {
        var chiavi = await ChiaviDocumenti("""{ "quarters": ["2026_1 "] }""");

        Assert.That(chiavi, Is.EqualTo(new[] { "APPIO-OB1|2026_1" }));
    }

    /// <summary>
    /// T16 — Se anno e trimestre non concordano prevale il trimestre: con anno 2025 e trimestre
    /// 2026_1 arrivano i documenti del 2026_1.
    /// </summary>
    [Test]
    public async Task T16_TrimestreDiUnAltroAnno_ShouldPrevalereSullAnno_Caratterizzazione()
    {
        var chiavi = await ChiaviDocumenti("""{ "year": "2025", "quarters": ["2026_1"] }""");

        Assert.That(chiavi, Is.EqualTo(new[] { "APPIO-OB1|2026_1" }));
    }

    /// <summary>
    /// T17 — Il codice fiscale è confrontato per intero: una parte, spazi in testa o un codice che
    /// il filtro non propone (ente senza documenti) non trovano nulla.
    /// </summary>
    [TestCase("0000000000")]
    [TestCase(" 00000000002")]
    [TestCase("00000000003")]
    [TestCase("00000000091")]
    [TestCase("%")]
    public async Task T17_CodiceFiscaleNonValido_ShouldReturn404(string codice)
    {
        var resp = await Post(Documenti, J(new { year = "2026", codiciFiscali = new[] { codice } }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T18 — Un ente dell'anagrafica senza documenti (l'acquedotto del testbook) è proposto
    /// dall'autocomplete, che è lo stesso dell'anagrafica, ma in griglia non trova nulla.
    /// </summary>
    [Test]
    public async Task T18_EnteSenzaDocumenti_ShouldEssereProponibileMaSenzaRisultati_Caratterizzazione()
    {
        var ricerca = await Post($"{Contratti}/name", J(new { name = AppIoTestbookDati.RicercaEnte }));
        var griglia = await Post(Documenti, J(new { year = "2026", institutionIds = new[] { AppIoTestbookDati.EnteAcquedotto } }));

        Assert.Multiple(() =>
        {
            Assert.That(ricerca.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// T19 — Senza anno né trimestre gli altri filtri lavorano sul solo trimestre più recente: un
    /// ente che ha documenti solo in trimestri precedenti (ENTE-4, 2025_4) non compare.
    /// </summary>
    [Test]
    public async Task T19_FiltriSenzaPeriodo_ShouldLavorareSulTrimestrePiuRecente_Caratterizzazione()
    {
        var senzaPeriodo = await Post(Documenti, """{ "institutionIds": ["APPIO-ENTE-4"] }""");
        var conAnno = await ChiaviDocumenti("""{ "year": "2025", "institutionIds": ["APPIO-ENTE-4"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(senzaPeriodo.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(conAnno, Is.EqualTo(new[] { "APPIO-OB4|2025_4" }));
        });
    }

    // ============================================================== T21–T25: dettaglio

    /// <summary>
    /// T21 — Il dettaglio si apre solo con contratto e trimestre di una riga esistente: valori
    /// scambiati, di un contratto senza documenti, in formato errato o ostili danno 404.
    /// </summary>
    [TestCase("2026_2", "APPIO-OB1", TestName = "T21_DettaglioConValoriScambiati_ShouldReturn404")]
    [TestCase("APPIO-TB1", "2026_2", TestName = "T21_DettaglioDiContrattoSenzaDocumenti_ShouldReturn404")]
    [TestCase("APPIO-OB1", "Q2", TestName = "T21_DettaglioConEtichettaDelTrimestre_ShouldReturn404")]
    [TestCase("APPIO-OB1", "2026_3", TestName = "T21_DettaglioDiTrimestreSenzaDocumenti_ShouldReturn404")]
    [TestCase("APPIO-OB9", "2026_2", TestName = "T21_DettaglioDiContrattoOrfano_ShouldReturn404")]
    [TestCase("APPIO-OB1' OR '1'='1", "2026_2", TestName = "T21_DettaglioConInjection_ShouldReturn404")]
    [TestCase("APPIO-ENTE-1", "2026_2", TestName = "T21_DettaglioConIdEnteAlPostoDelContratto_ShouldReturn404")]
    public async Task T21_DettaglioDiUnaRigaInesistente_ShouldReturn404(string contratto, string trimestre)
    {
        var body = J(new { contractId = contratto, yearQuarter = trimestre });

        var dettaglio = await Post($"{Documenti}/detail", body);
        var download = await Post($"{Documenti}/detail/download", body);

        Assert.Multiple(() =>
        {
            Assert.That(dettaglio.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// T21 — La chiave della riga ("contratto|trimestre") non è un parametro del dettaglio: mandata
    /// al posto di contractId non apre nulla.
    /// </summary>
    [Test]
    public async Task T21_DettaglioConLaChiaveDellaRiga_ShouldReturn404()
    {
        var resp = await Post($"{Documenti}/detail", """{ "contractId": "APPIO-OB1|2026_2", "yearQuarter": "2026_2" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// T22 — Il contratto è confrontato senza distinguere maiuscole e minuscole (collation del
    /// database): "appio-ob1" apre lo stesso dettaglio.
    /// </summary>
    [Test]
    public async Task T22_ContrattoInMinuscolo_ShouldAprireLoStessoDettaglio_Caratterizzazione()
    {
        var resp = await Post($"{Documenti}/detail", """{ "contractId": "appio-ob1", "yearQuarter": "2026_2" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Json(resp)).GetProperty("contractId").GetString(), Is.EqualTo("APPIO-OB1"));
    }

    /// <summary>
    /// T23 — Il totale del dettaglio è quello del solo contratto nel trimestre: i documenti di un
    /// altro contratto dello stesso trimestre (OB2, 2) non entrano nei 25 di OB1.
    /// </summary>
    [Test]
    public async Task T23_ImportoTotale_ShouldEssereDelSoloContratto()
    {
        var ob1 = await Json(await Post($"{Documenti}/detail", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_2" }"""));
        var ob2 = await Json(await Post($"{Documenti}/detail", """{ "contractId": "APPIO-OB2", "yearQuarter": "2026_2" }"""));

        Assert.Multiple(() =>
        {
            Assert.That(ob1.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(25m));
            Assert.That(ob2.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(2m));
        });
    }

    /// <summary>
    /// T25 — Il download dal blob non accetta un percorso dal client e non lo restituisce: allo
    /// storage arriva solo il percorso letto dalla vista, anche con campi ostili nel body.
    /// </summary>
    [Test]
    public async Task T25_DownloadConPercorsoIndicatoDalClient_ShouldUsareSoloQuelloDellaVista()
    {
        var resp = await Post($"{Documenti}/blob/download",
            """{ "year": 2026, "quarter": 1, "filePath": "abfss://altro@altro.dfs.core.windows.net/x.csv", "contractId": "APPIO-OB9" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(FakeAppIoFinancialReportStorageService.Instance.Richieste.Single(), Does.EndWith("export/sap/2026/1/appio-final_report-2026-q1.csv"));
    }

    // =================================================== tutte le rotte usate dal testbook

    private static readonly (string Metodo, string Rotta, string Body, string Query)[] RotteDelTestbook =
    [
        ("GET", $"{Contratti}/tipoente", "", ""),
        ("POST", $"{Contratti}/name", """{ "name": "acquedotto" }""", ""),
        ("POST", Contratti, "{}", "&page=1&pageSize=10"),
        ("POST", $"{Contratti}/download", "{}", ""),
        ("GET", $"{Documenti}/years", "", ""),
        ("POST", $"{Documenti}/quarters", """{ "year": "2026" }""", ""),
        ("GET", $"{Documenti}/codfiscale", "", ""),
        ("POST", Documenti, "{}", ""),
        ("POST", $"{Documenti}/document", "{}", ""),
        ("POST", $"{Documenti}/detail", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_2" }""", ""),
        ("POST", $"{Documenti}/detail/download", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_2" }""", ""),
        ("POST", $"{Documenti}/blob/download", """{ "year": 2026, "quarter": 2 }""", "")
    ];

    private async Task<List<(string Rotta, HttpStatusCode Esito)>> ChiamaTutte(HttpClient client, string prodottoNonce = "prod-pn")
    {
        var esiti = new List<(string, HttpStatusCode)>();
        foreach (var (metodo, rotta, body, query) in RotteDelTestbook)
        {
            var url = _factory.WithNonce(rotta, prodotto: prodottoNonce) + query;
            var resp = metodo == "GET"
                ? await client.GetAsync(url)
                : await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            esiti.Add((rotta, resp.StatusCode));
        }
        return esiti;
    }

    /// <summary>
    /// Il testbook indica l'utente ADMIN, ma tutte le sue chiamate sono letture: le esegue anche un
    /// OPERATOR, con lo stesso esito.
    /// </summary>
    [Test]
    public async Task Testbook_Operator_ShouldPoterEseguireTutteLeChiamate()
    {
        var esiti = await ChiamaTutte(_factory.CreateClientAs(Ruolo.OPERATOR));

        Assert.That(esiti.Select(x => x.Esito), Is.All.EqualTo(HttpStatusCode.OK), string.Join(", ", esiti));
    }

    /// <summary>
    /// Nessuna chiamata del testbook risponde senza autenticazione.
    /// </summary>
    [Test]
    public async Task Testbook_Anonimo_ShouldRicevere401SuTutteLeChiamate()
    {
        var esiti = await ChiamaTutte(_factory.CreateClientAs(null));

        Assert.That(esiti.Select(x => x.Esito), Is.All.EqualTo(HttpStatusCode.Unauthorized), string.Join(", ", esiti));
    }

    /// <summary>
    /// Un aderente SelfCare non accede alle pagine APP IO del pannello amministratore.
    /// </summary>
    [Test]
    public async Task Testbook_UtenteSelfCare_ShouldRicevere403SuTutteLeChiamate()
    {
        var esiti = await ChiamaTutte(_factory.CreateClientAs(Ruolo.ADMIN, auth: "SELFCARE", profilo: "PA"));

        Assert.That(esiti.Select(x => x.Esito), Is.All.EqualTo(HttpStatusCode.Forbidden), string.Join(", ", esiti));
    }

    /// <summary>
    /// Con il nonce di un altro prodotto ogni chiamata del testbook è rifiutata con 419.
    /// </summary>
    [Test]
    public async Task Testbook_NonceDiUnAltroProdotto_ShouldRicevere419SuTutteLeChiamate()
    {
        var esiti = await ChiamaTutte(_factory.CreateClientAs(Ruolo.ADMIN), prodottoNonce: "prod-appio");

        Assert.That(esiti.Select(x => (int)x.Esito), Is.All.EqualTo(419), string.Join(", ", esiti));
    }
}
