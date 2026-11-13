using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il testbook APP IO (PF-550_TESTBOOK_APP-IO, foglio "TESTBOOK Admin") eseguito sulle API, un test
/// per caso: il nome di ogni test inizia con l'ID del caso (T01…T25), così l'esito si riporta 1:1 nel
/// giro di test. Il testbook è scritto per la UI; qui si verifica ciò che la pagina chiede al
/// backend e ciò che riceve.
///
/// Dati: seed di tests/Data/appio.sql più i due contratti di <see cref="AppIoTestbookDati"/>
/// (un "ACQUEDOTTO…" GSP e una banca PSP), che riproducono i valori usati dal testbook su UAT.
/// Contratti: OB1 (ENTE-1, "Comune Alfa AppIO Test", PA), OB2 (ENTE-2, "Comune Beta AppIO Test", PA),
/// OB3 (ENTE-3, "Regione Gamma", PA, senza PEC), OB4 (ENTE-4, omonimo di OB1, GSP), OB5 (senza nome
/// né tipo), TB1 (GSP), TB2 (PSP). Documenti: 2026_2 → OB1 (25) e OB2 (2); 2026_1 → OB1 (3);
/// 2025_4 → OB4 (1).
///
/// Non automatizzabile qui: <b>T26</b> (pulsante Indietro e filtri mantenuti), l'aspetto delle
/// pagine e ciò che si vede solo aprendo i file in Excel. Gli input ostili sugli stessi casi sono in
/// <see cref="AppIoTestbookAdversarialHttpTests"/>.
/// </summary>
[Category("AppIO")]
[Category("TestbookAppIO")]
public class AppIoTestbookHttpTests
{
    private const string Contratti = "/api/appio/contracts";
    private const string Documenti = "/api/appio/financialreports";
    private const string Pagina = "&page=1&pageSize=10";

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

    [TearDown]
    public void ResetStorage() => FakeAppIoFinancialReportStorageService.Instance.Reset();

    // Il testbook è eseguito da un utente ADMIN
    private async Task<HttpResponseMessage> Post(string rotta, string body = "{}", string query = "")
    {
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await _factory.CreateClientAs(Ruolo.ADMIN).PostAsync(_factory.WithNonce(rotta) + query, content);
        TestContext.Out.WriteLine($"POST {rotta}{query} {body} -> {(int)resp.StatusCode}");
        return resp;
    }

    private async Task<HttpResponseMessage> Get(string rotta)
        => await _factory.CreateClientAs(Ruolo.ADMIN).GetAsync(_factory.WithNonce(rotta));

    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    private static List<string?> Valori(JsonElement array, string proprieta)
        => array.EnumerateArray().Select(x => x.GetProperty(proprieta).GetString()).ToList();

    private static List<string?> Stringhe(JsonElement array)
        => array.EnumerateArray().Select(x => x.GetString()).ToList();

    private static async Task<string> TestoExcel(HttpResponseMessage resp)
    {
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        return string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));
    }

    /// <summary>Righe di dati di un foglio: tutte le righe meno l'intestazione.</summary>
    private static int RigheDati(string foglio) => Regex.Matches(foglio, @"<(?:\w+:)?row\b").Count - 1;

    private async Task<JsonElement> Griglia(string body)
    {
        var resp = await Post(Contratti, body, Pagina);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
        return await Json(resp);
    }

    private async Task<JsonElement> GrigliaDocumenti(string body)
    {
        var resp = await Post(Documenti, body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
        return (await Json(resp)).GetProperty("financialReports");
    }

    // ===================================================================== Anagrafica AppIO

    /// <summary>
    /// T01 — Nessun filtro: la pagina mostra i filtri, il download e la griglia popolata. Le quattro
    /// chiamate della pagina rispondono, e la griglia senza filtri contiene tutti i contratti.
    /// </summary>
    [Test]
    public async Task T01_AnagraficaSenzaFiltri_ShouldMostrareFiltriDownloadEGrigliaPopolata()
    {
        var griglia = await Griglia("{}");
        var tipi = await Get($"{Contratti}/tipoente");
        var nomi = await Post($"{Contratti}/name");
        var download = await Post($"{Contratti}/download");

        Assert.Multiple(() =>
        {
            Assert.That(griglia.GetProperty("count").GetInt32(), Is.EqualTo(AppIoTestbookDati.ContrattiTotali));
            Assert.That(griglia.GetProperty("contratti").GetArrayLength(), Is.EqualTo(AppIoTestbookDati.ContrattiTotali));
            Assert.That(tipi.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Tipo Profilo");
            Assert.That(nomi.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Ente");
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK), "download risultati");
        });
    }

    /// <summary>
    /// T02 — Apertura del filtro Tipo Profilo: i valori GSP, PA e PSP.
    /// </summary>
    [Test]
    public async Task T02_FiltroTipoProfilo_ShouldProporreGspPaEPsp()
    {
        var resp = await Get($"{Contratti}/tipoente");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Stringhe(await Json(resp)), Is.EqualTo(new[] { "GSP", "PA", "PSP" }));
    }

    /// <summary>
    /// T03, T04, T05 — Tipo Profilo = PA, GSP, PSP: solo gli enti di quel profilo.
    /// </summary>
    [TestCase("PA", new[] { "APPIO-OB1", "APPIO-OB2", "APPIO-OB3" }, TestName = "T03_TipoProfiloPA_ShouldMostrareSoloEntiPA")]
    [TestCase("GSP", new[] { "APPIO-TB1", "APPIO-OB4" }, TestName = "T04_TipoProfiloGSP_ShouldMostrareSoloEntiGSP")]
    [TestCase("PSP", new[] { "APPIO-TB2" }, TestName = "T05_TipoProfiloPSP_ShouldMostrareSoloEntiPSP")]
    public async Task T03_T04_T05_TipoProfilo_ShouldMostrareSoloGliEntiDelProfilo(string tipo, string[] attesi)
    {
        var griglia = await Griglia($$"""{ "tipiEnte": ["{{tipo}}"] }""");
        var righe = griglia.GetProperty("contratti");

        Assert.Multiple(() =>
        {
            Assert.That(Valori(righe, "institutionType"), Is.All.EqualTo(tipo));
            Assert.That(Valori(righe, "contractId"), Is.EquivalentTo(attesi));
            Assert.That(griglia.GetProperty("count").GetInt32(), Is.EqualTo(attesi.Length));
        });
    }

    /// <summary>
    /// T06 — Ricerca ente = "acquedotto": l'autocomplete propone l'acquedotto, e solo quello.
    /// </summary>
    [Test]
    public async Task T06_RicercaEnteAcquedotto_ShouldValorizzareLAutocomplete()
    {
        var resp = await Post($"{Contratti}/name", $$"""{ "name": "{{AppIoTestbookDati.RicercaEnte}}" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var voci = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(voci, "name"), Is.EqualTo(new[] { AppIoTestbookDati.NomeAcquedotto }));
            Assert.That(Valori(voci, "institutionId"), Is.EqualTo(new[] { AppIoTestbookDati.EnteAcquedotto }));
        });
    }

    /// <summary>
    /// T07 — Ente selezionato dall'autocomplete: la griglia mostra solo quel record. L'id usato è
    /// quello restituito dalla ricerca, come fa la pagina.
    /// </summary>
    [Test]
    public async Task T07_EnteSelezionato_ShouldMostrareSoloQuelRecord()
    {
        var voci = await Json(await Post($"{Contratti}/name", $$"""{ "name": "{{AppIoTestbookDati.RicercaEnte}}" }"""));
        var id = voci[0].GetProperty("institutionId").GetString();

        var griglia = await Griglia($$"""{ "institutionIds": ["{{id}}"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(griglia.GetProperty("count").GetInt32(), Is.EqualTo(1));
            Assert.That(Valori(griglia.GetProperty("contratti"), "name"), Is.EqualTo(new[] { AppIoTestbookDati.NomeAcquedotto }));
            Assert.That(Valori(griglia.GetProperty("contratti"), "contractId"), Is.EqualTo(new[] { AppIoTestbookDati.ContrattoAcquedotto }));
        });
    }

    /// <summary>
    /// T08 — Tipo Profilo + Ente: solo i record coerenti con entrambi. L'acquedotto è GSP: con GSP
    /// compare, con PA no (i due filtri sono in AND).
    /// </summary>
    [Test]
    public async Task T08_TipoProfiloPiuEnte_ShouldMostrareSoloIRecordCoerentiConEntrambi()
    {
        var coerente = await Griglia($$"""{ "tipiEnte": ["GSP"], "institutionIds": ["{{AppIoTestbookDati.EnteAcquedotto}}"] }""");
        var incoerente = await Post(Contratti, $$"""{ "tipiEnte": ["PA"], "institutionIds": ["{{AppIoTestbookDati.EnteAcquedotto}}"] }""", Pagina);

        Assert.Multiple(() =>
        {
            Assert.That(Valori(coerente.GetProperty("contratti"), "contractId"), Is.EqualTo(new[] { AppIoTestbookDati.ContrattoAcquedotto }));
            Assert.That(incoerente.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "profilo ed ente non compatibili");
        });
    }

    /// <summary>
    /// T09 — Ente inesistente: nessun risultato, cioè 404 su autocomplete, griglia e download.
    /// </summary>
    [Test]
    public async Task T09_EnteInesistente_ShouldNonRestituireRisultati()
    {
        var ricerca = await Post($"{Contratti}/name", """{ "name": "ente che non esiste" }""");
        var griglia = await Post(Contratti, """{ "institutionIds": ["ENTE-INESISTENTE"] }""", Pagina);
        var download = await Post($"{Contratti}/download", """{ "institutionIds": ["ENTE-INESISTENTE"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(ricerca.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// T10 — Download risultati senza filtri: un .xlsx con tutti i record della griglia, anche oltre
    /// la pagina visualizzata. Le colonne sono quelle dell'export, compreso ContractState.
    /// </summary>
    [Test]
    public async Task T10_DownloadSenzaFiltri_ShouldEsportareTuttiIRecord()
    {
        var griglia = await Json(await Post(Contratti, "{}", "&page=1&pageSize=2"));
        var resp = await Post($"{Contratti}/download");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        var foglio = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(griglia.GetProperty("contratti").GetArrayLength(), Is.EqualTo(2), "la griglia è paginata");
            Assert.That(RigheDati(foglio), Is.EqualTo(griglia.GetProperty("count").GetInt32()), "l'export non è paginato");
            foreach (var colonna in new[] { "ContractId", "InstitutionId", "Name", "Prodotto", "InstitutionType", "EmailPec", "ContractState" })
                Assert.That(foglio, Does.Contain(colonna));
            Assert.That(foglio, Does.Contain("SUSPENDED"), "l'export contiene anche i contratti non attivi");
        });
    }

    /// <summary>
    /// T11 — Download risultati con filtri: solo i record filtrati, gli stessi della griglia.
    /// </summary>
    [Test]
    public async Task T11_DownloadConFiltri_ShouldEsportareSoloIRecordFiltrati()
    {
        const string filtro = """{ "tipiEnte": ["GSP"] }""";
        var griglia = await Griglia(filtro);

        var resp = await Post($"{Contratti}/download", filtro);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var foglio = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(RigheDati(foglio), Is.EqualTo(griglia.GetProperty("count").GetInt32()));
            Assert.That(foglio, Does.Contain(AppIoTestbookDati.NomeAcquedotto));
            Assert.That(foglio, Does.Not.Contain("Comune Beta AppIO Test"), "Comune Beta è PA");
            Assert.That(foglio, Does.Not.Contain(AppIoTestbookDati.NomeBanca), "la banca è PSP");
        });
    }

    /// <summary>
    /// T12 — Colonne della griglia: Ente, ID Contratto ed E-Mail Ref. Fattura valorizzati. Un contratto
    /// senza indirizzo PEC (OB3) arriva con l'e-mail nulla, senza rompere la griglia.
    /// </summary>
    [Test]
    public async Task T12_ColonneDellaGriglia_ShouldEssereValorizzate()
    {
        var righe = (await Griglia("{}")).GetProperty("contratti").EnumerateArray().ToList();
        var acquedotto = righe.Single(x => x.GetProperty("contractId").GetString() == AppIoTestbookDati.ContrattoAcquedotto);
        var senzaPec = righe.Single(x => x.GetProperty("contractId").GetString() == "APPIO-OB3");

        Assert.Multiple(() =>
        {
            Assert.That(acquedotto.GetProperty("name").GetString(), Is.EqualTo(AppIoTestbookDati.NomeAcquedotto), "Ente");
            Assert.That(acquedotto.GetProperty("emailPec").GetString(), Is.EqualTo(AppIoTestbookDati.PecAcquedotto), "E-Mail Ref. Fattura");
            Assert.That(righe.Select(x => x.GetProperty("contractId").GetString()), Is.All.Not.Empty, "ID Contratto");
            Assert.That(senzaPec.GetProperty("emailPec").ValueKind, Is.EqualTo(JsonValueKind.Null));
            foreach (var riga in righe)
                foreach (var colonna in new[] { "institutionId", "prodotto", "institutionType", "contractState" })
                    Assert.That(riga.TryGetProperty(colonna, out _), Is.True, colonna);
        });
    }

    // ================================================================== Documenti contabili

    /// <summary>
    /// T13 — Accesso alla pagina: i filtri Anno, Trimestre, Codice Fiscale ed Ente rispondono e la
    /// griglia, senza filtri, mostra il trimestre più recente.
    /// </summary>
    [Test]
    public async Task T13_AccessoPaginaDocumenti_ShouldMostrareFiltriEGriglia()
    {
        var anni = await Get($"{Documenti}/years");
        var trimestri = await Post($"{Documenti}/quarters");
        var codici = await Get($"{Documenti}/codfiscale");
        var enti = await Post($"{Contratti}/name");
        var griglia = await GrigliaDocumenti("{}");

        Assert.Multiple(async () =>
        {
            Assert.That(Stringhe(await Json(anni)), Is.EqualTo(new[] { "2026", "2025" }), "filtro Anno");
            Assert.That(trimestri.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Trimestre");
            Assert.That(Stringhe(await Json(codici)), Is.EqualTo(new[] { "00000000001", "00000000002", "00000000004" }), "filtro Codice Fiscale: solo enti con documenti");
            Assert.That(enti.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Ente");
            Assert.That(Valori(griglia, "key"), Is.EqualTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB2|2026_2" }), "griglia: trimestre più recente");
        });
    }

    /// <summary>
    /// T14 — Anno = 2026: i documenti dell'anno, di tutti i suoi trimestri.
    /// </summary>
    [Test]
    public async Task T14_Anno2026_ShouldMostrareIDocumentiDellAnno()
    {
        var righe = await GrigliaDocumenti("""{ "year": "2026" }""");

        Assert.Multiple(() =>
        {
            Assert.That(righe.EnumerateArray().Select(x => x.GetProperty("year").GetInt32()), Is.All.EqualTo(2026));
            Assert.That(Valori(righe, "key"), Is.EquivalentTo(new[] { "APPIO-OB1|2026_2", "APPIO-OB1|2026_1", "APPIO-OB2|2026_2" }));
        });
    }

    /// <summary>
    /// T15 — Apertura del filtro Trimestre: i trimestri disponibili dell'anno scelto, con etichetta Qn.
    /// </summary>
    [Test]
    public async Task T15_FiltroTrimestre_ShouldProporreITrimestriDisponibili()
    {
        var resp = await Post($"{Documenti}/quarters", """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var voci = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(voci, "quarter"), Is.EqualTo(new[] { "Q1", "Q2" }));
            Assert.That(Valori(voci, "value"), Is.EqualTo(new[] { "2026_1", "2026_2" }));
        });
    }

    /// <summary>
    /// T16 — Trimestre = Q1: solo i documenti del trimestre. Il valore inviato è quello della voce
    /// Q1 del filtro, come fa la pagina.
    /// </summary>
    [Test]
    public async Task T16_TrimestreQ1_ShouldMostrareSoloIDocumentiDelTrimestre()
    {
        var voci = await Json(await Post($"{Documenti}/quarters", """{ "year": "2026" }"""));
        var q1 = voci.EnumerateArray().Single(x => x.GetProperty("quarter").GetString() == "Q1").GetProperty("value").GetString();

        var righe = await GrigliaDocumenti($$"""{ "year": "2026", "quarters": ["{{q1}}"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(Valori(righe, "yearQuarter"), Is.All.EqualTo("2026_1"));
            Assert.That(Valori(righe, "key"), Is.EqualTo(new[] { "APPIO-OB1|2026_1" }));
        });
    }

    /// <summary>
    /// T17 — Filtro Codice Fiscale: risultati coerenti con il codice scelto. Vale per ogni codice
    /// proposto dal filtro: nessuno porta a una griglia vuota.
    /// </summary>
    [Test]
    public async Task T17_FiltroCodiceFiscale_ShouldMostrareRisultatiCoerenti()
    {
        var codici = Stringhe(await Json(await Get($"{Documenti}/codfiscale")));
        var anni = Stringhe(await Json(await Get($"{Documenti}/years")));

        foreach (var codice in codici)
        {
            var trovate = new List<string?>();
            foreach (var anno in anni)
            {
                var resp = await Post(Documenti, $$"""{ "year": "{{anno}}", "codiciFiscali": ["{{codice}}"] }""");
                if (resp.StatusCode == HttpStatusCode.OK)
                    trovate.AddRange(Valori((await Json(resp)).GetProperty("financialReports"), "codiceFiscale"));
            }

            Assert.That(trovate, Is.Not.Empty.And.All.EqualTo(codice), $"codice fiscale {codice}");
        }
    }

    /// <summary>
    /// T18 — Filtro Ente: risultati coerenti con l'ente scelto dall'autocomplete.
    /// </summary>
    [Test]
    public async Task T18_FiltroEnte_ShouldMostrareRisultatiCoerenti()
    {
        var voci = await Json(await Post($"{Contratti}/name", """{ "name": "Beta" }"""));
        var id = voci.EnumerateArray().Single().GetProperty("institutionId").GetString();

        var righe = await GrigliaDocumenti($$"""{ "year": "2026", "institutionIds": ["{{id}}"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(Valori(righe, "institutionId"), Is.All.EqualTo("APPIO-ENTE-2"));
            Assert.That(Valori(righe, "name"), Is.All.EqualTo("Comune Beta AppIO Test"));
        });
    }

    /// <summary>
    /// T19 — Combinazione di filtri (Ente + Anno + Trimestre): solo i record coerenti con tutti.
    /// Aggiungendo anche un codice fiscale di un altro ente non resta nulla.
    /// </summary>
    [Test]
    public async Task T19_CombinazioneFiltri_ShouldMostrareSoloIRecordCoerentiConTutti()
    {
        var righe = await GrigliaDocumenti("""{ "year": "2026", "quarters": ["2026_1"], "institutionIds": ["APPIO-ENTE-1"] }""");
        var incoerente = await Post(Documenti, """{ "year": "2026", "quarters": ["2026_1"], "institutionIds": ["APPIO-ENTE-1"], "codiciFiscali": ["00000000002"] }""");

        Assert.Multiple(() =>
        {
            Assert.That(Valori(righe, "key"), Is.EqualTo(new[] { "APPIO-OB1|2026_1" }));
            Assert.That(incoerente.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// T20 — Download risultati: il file contiene i risultati filtrati, una riga per riga di documento
    /// (tante quante le posizioni della griglia), con gli importi in formato valuta.
    /// </summary>
    [Test]
    public async Task T20_DownloadRisultati_ShouldContenereIRisultatiFiltrati()
    {
        const string filtro = """{ "year": "2026", "institutionIds": ["APPIO-ENTE-1"] }""";
        var posizioni = (await GrigliaDocumenti(filtro)).EnumerateArray().Sum(x => x.GetProperty("posizioni").GetArrayLength());

        var resp = await Post($"{Documenti}/document", filtro);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var foglio = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(RigheDati(foglio), Is.EqualTo(posizioni));
            Assert.That(foglio, Does.Contain("APPIO-FR-001").And.Contain("APPIO-FR-004"));
            Assert.That(foglio, Does.Not.Contain("APPIO-FR-003"), "FR-003 è di un altro ente");
            Assert.That(Regex.Matches(foglio, "s=\"3\"").Count, Is.EqualTo(posizioni * 2), "ImportoTotale e Importo in formato valuta");
        });
    }

    // ======================================================== Dettaglio documento contabile

    /// <summary>Apre il dettaglio della prima riga della griglia, come un clic sulla riga.</summary>
    private async Task<(JsonElement Riga, JsonElement Dettaglio)> ApriDettaglioDellaPrimaRiga()
    {
        var riga = (await GrigliaDocumenti("{}"))[0];
        var body = $$"""{ "contractId": "{{riga.GetProperty("contractId").GetString()}}", "yearQuarter": "{{riga.GetProperty("yearQuarter").GetString()}}" }""";
        var resp = await Post($"{Documenti}/detail", body);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
        return (riga, await Json(resp));
    }

    /// <summary>
    /// T21 — Selezione di un documento dalla griglia: i valori della riga (contractId, yearQuarter)
    /// aprono il dettaglio di quel documento.
    /// </summary>
    [Test]
    public async Task T21_SelezioneDocumentoDallaGriglia_ShouldAprireIlDettaglio()
    {
        var (riga, dettaglio) = await ApriDettaglioDellaPrimaRiga();

        Assert.Multiple(() =>
        {
            Assert.That(dettaglio.GetProperty("contractId").GetString(), Is.EqualTo(riga.GetProperty("contractId").GetString()));
            Assert.That(dettaglio.GetProperty("yearQuarter").GetString(), Is.EqualTo(riga.GetProperty("yearQuarter").GetString()));
            Assert.That(dettaglio.GetProperty("name").GetString(), Is.EqualTo(riga.GetProperty("name").GetString()));
        });
    }

    /// <summary>
    /// T22 — Dati del documento: Ente, Partita IVA, Codice Fiscale, ID Contratto, Prodotto e Piano Tariffario.
    /// </summary>
    [Test]
    public async Task T22_AperturaDettaglio_ShouldMostrareIDatiDelDocumento()
    {
        var (_, d) = await ApriDettaglioDellaPrimaRiga();

        Assert.Multiple(() =>
        {
            Assert.That(d.GetProperty("name").GetString(), Is.EqualTo("Comune Alfa AppIO Test"), "Ente");
            Assert.That(d.GetProperty("partitaIva").GetString(), Is.EqualTo("00000000001"), "Partita IVA");
            Assert.That(d.GetProperty("codiceFiscale").GetString(), Is.EqualTo("00000000001"), "Codice Fiscale");
            Assert.That(d.GetProperty("contractId").GetString(), Is.EqualTo("APPIO-OB1"), "ID Contratto");
            Assert.That(d.GetProperty("prodotto").GetString(), Is.EqualTo("prod-io"), "Prodotto");
            Assert.That(d.GetProperty("pricingPlan").GetString(), Is.EqualTo("FA"), "Piano Tariffario");
        });
    }

    /// <summary>
    /// T23 — Dati economici: Trimestre, Importo Totale (due decimali, uguale a quello della griglia),
    /// Data Creazione e Ultimo Aggiornamento.
    /// </summary>
    [Test]
    public async Task T23_DatiEconomici_ShouldEssereCorretti()
    {
        var (riga, d) = await ApriDettaglioDellaPrimaRiga();
        var totale = d.GetProperty("importoTotale").GetDecimal();

        Assert.Multiple(() =>
        {
            Assert.That(d.GetProperty("yearQuarter").GetString(), Is.EqualTo("2026_2"), "Trimestre");
            Assert.That(d.GetProperty("year").GetInt32(), Is.EqualTo(2026));
            Assert.That(d.GetProperty("quarter").GetInt32(), Is.EqualTo(2));
            Assert.That(totale, Is.EqualTo(25m), "Importo Totale");
            Assert.That(decimal.Round(totale, 2), Is.EqualTo(totale), "al più due decimali");
            Assert.That(totale, Is.EqualTo(decimal.Round(riga.GetProperty("importoTotale").GetDecimal(), 2)), "uguale alla griglia");
            Assert.That(d.GetProperty("dataCreazione").GetDateTime(), Is.EqualTo(new DateTime(2025, 1, 10)), "Data Creazione");
            Assert.That(d.GetProperty("ultimoAggiornamento").GetDateTime(), Is.EqualTo(new DateTime(2026, 3, 1)), "Ultimo Aggiornamento");
        });
    }

    /// <summary>
    /// T24 — Sezione Posizioni: una riga per riga di documento, con Codice Articolo, Quantità e
    /// Importo; la somma degli importi è l'Importo Totale. ("ID Categoria" del testbook non è un campo
    /// dell'API: le posizioni espongono numero e tipo del documento.)
    /// </summary>
    [Test]
    public async Task T24_SezionePosizioni_ShouldMostrareArticoloQuantitaEImporto()
    {
        var (_, d) = await ApriDettaglioDellaPrimaRiga();
        var posizioni = d.GetProperty("posizioni").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(posizioni.Select(p => p.GetProperty("codiceArticolo").GetString()), Is.EqualTo(new[] { "ART-A", "ART-B", "ART-A" }));
            Assert.That(posizioni.Select(p => p.GetProperty("quantita").GetInt64()), Is.EqualTo(new long[] { 1000, 500, 500 }));
            Assert.That(posizioni.Select(p => p.GetProperty("importo").GetDecimal()), Is.EqualTo(new[] { 10m, 10m, 5m }));
            Assert.That(posizioni.Sum(p => p.GetProperty("importo").GetDecimal()), Is.EqualTo(d.GetProperty("importoTotale").GetDecimal()));
            Assert.That(posizioni.Select(p => p.GetProperty("numero").GetString()), Is.EqualTo(new[] { "APPIO-FR-001", "APPIO-FR-001", "APPIO-FR-002" }));
        });
    }

    /// <summary>
    /// T25 — Download Financial Report dal dettaglio, nelle sue due forme: l'Excel del dettaglio (una
    /// riga per posizione, importi in formato valuta) e il file del trimestre letto dal blob storage,
    /// consegnato con un nome costruito dal trimestre e senza il percorso sullo storage.
    /// </summary>
    [Test]
    public async Task T25_DownloadFinancialReport_ShouldScaricareIlFileDelDocumento()
    {
        var (riga, d) = await ApriDettaglioDellaPrimaRiga();
        var contratto = riga.GetProperty("contractId").GetString();

        var excel = await Post($"{Documenti}/detail/download", $$"""{ "contractId": "{{contratto}}", "yearQuarter": "2026_2" }""");
        var blob = await Post($"{Documenti}/blob/download", """{ "year": 2026, "quarter": 2 }""");

        Assert.That(excel.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(blob.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var foglio = await TestoExcel(excel);
        var intestazioni = blob.Headers + blob.Content.Headers.ToString();
        Assert.Multiple(async () =>
        {
            Assert.That(RigheDati(foglio), Is.EqualTo(d.GetProperty("posizioni").GetArrayLength()));
            Assert.That(Regex.Matches(foglio, "s=\"3\"").Count, Is.EqualTo(6), "ImportoTotale e Importo in formato valuta, tre righe");
            Assert.That(Regex.Matches(foglio, "s=\"9\"").Count, Is.EqualTo(0), "nessun importo a 14 decimali");
            Assert.That(await blob.Content.ReadAsStringAsync(), Is.EqualTo(FakeAppIoFinancialReportStorageService.Contenuto));
            Assert.That(blob.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Is.EqualTo("financial-report-appio-2026_2.csv"));
            Assert.That(intestazioni, Does.Not.Contain("abfss").And.Not.Contain("export/sap"));
        });
    }

    /// <summary>
    /// T25 — Con lo storage non configurato (stato attuale degli ambienti) il download dal blob
    /// risponde 503, mentre l'Excel del dettaglio continua a funzionare.
    /// </summary>
    [Test]
    public async Task T25_DownloadFinancialReport_StorageNonConfigurato_ShouldReturn503SoloSulBlob()
    {
        FakeAppIoFinancialReportStorageService.Instance.Leggibile = false;

        var blob = await Post($"{Documenti}/blob/download", """{ "year": 2026, "quarter": 2 }""");
        var excel = await Post($"{Documenti}/detail/download", """{ "contractId": "APPIO-OB1", "yearQuarter": "2026_2" }""");

        Assert.Multiple(() =>
        {
            Assert.That(blob.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(excel.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }
}
