using System.Net;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Il testbook della pagina "Invio fatture a SAP" (PF-735/PF-965, casi T01–T51) eseguito sulle API, un
/// test per caso: il nome di ogni test inizia con l'ID del caso, così l'esito si riporta 1:1 nel giro
/// di test. Il testbook è scritto per la UI; qui si verifica ciò che la pagina chiede al backend e ciò
/// che riceve, compreso ciò che il backend scrive quando si conferma la generazione.
///
/// Dati: seed 2003 di tests/Data/invio_sap.sql. Righi della griglia: ANTICIPO 2003/1 inviate (9501);
/// PRIMO SALDO 2003/1 inviate (9502 con tre invii, 9503); PRIMO SALDO 2003/2 da inviare (9505, 9506),
/// inviate (9507) e in elaborazione (9508); SECONDO SALDO 2003/2 da inviare (9509); ACCONTO 2003/3 da
/// inviare (9510); VAR. SEMESTRALE 2003/7 inviate (9504). Synapse è finto e DATA è simulata da
/// <see cref="InvioSapTestData"/>.
///
/// Casi senza test, perché sono solo interfaccia: T09–T12 (griglia svuotata e bottoni abilitati al
/// cambio dei filtri), T18 (Annulla), T24 (filtri del dettaglio azzerati alla riapertura), T27, T29
/// (checkbox), T36 (ordinamento per colonna), T38, T39 (dialog di conferma). T42 e T43 (contenuto del
/// JSON) riguardano il file prodotto da DATA: qui si verifica solo la richiesta di generazione.
/// Gli input ostili sugli stessi casi sono in <see cref="InvioSapTestbookAdversarialHttpTests"/>.
/// </summary>
[Category("InvioSap")]
[Category("TestbookInvioSap")]
[NonParallelizable]
public class InvioSapTestbookHttpTests
{
    private const int Anno = InvioSapTestData.Anno;
    private const string RigoInviate = """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" }""";
    private const string RigoDaInviare = """{ "annoRiferimento": 2003, "meseRiferimento": 2, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "DA_INVIARE" }""";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        InvioSapTestData.SkipSeOggettiAssenti();
        _factory = new ApiTestFactory();
        InvioSapTestData.Pulisci();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        InvioSapTestData.Pulisci();
        FakeSynapseService.Instance.Reset();
        _factory?.Dispose();
    }

    // Lo stato del seed lo ripristina il TearDown di ogni test (e il OneTimeSetUp per il primo)
    [SetUp]
    public void Reset() => FakeSynapseService.Instance.Reset();

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    private Task<HttpResponseMessage> Griglia(string body) => Post(_factory, InvioSapTestData.RottaGriglia, body);
    private Task<HttpResponseMessage> Dettaglio(string body) => Post(_factory, InvioSapTestData.RottaDettaglio, body);

    /// <summary>Genera dopo che DATA ha rilasciato il lock del batch precedente (stato normale della pagina).</summary>
    private Task<HttpResponseMessage> Genera(object body)
    {
        if (InvioSapTestData.BatchDelLock() == InvioSapTestData.BatchErrore) InvioSapTestData.RilasciaLockComeData();
        return Put(_factory, InvioSapTestData.RottaGenera, JsonSerializer.Serialize(body, Web));
    }

    private static List<string> Id(JsonElement righe) => righe.EnumerateArray().Select(r => r.GetProperty("id").GetString()!).ToList();
    private static List<long> IdFatture(JsonElement dettaglio) => dettaglio.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("idFattura").GetInt64()).ToList();
    private static List<int> Interi(JsonElement lista) => lista.EnumerateArray().Select(e => e.GetInt32()).ToList();
    private static List<string> Stringhe(JsonElement lista) => lista.EnumerateArray().Select(e => e.GetString()!).ToList();

    private static List<List<string>> LeggiXlsx(byte[] contenuto)
    {
        using var ms = new MemoryStream(contenuto);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var wb = doc.WorkbookPart!;
        var shared = wb.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().Select(s => s.InnerText).ToList() ?? [];
        return wb.WorksheetParts.First().Worksheet.Descendants<Row>().Select(r => r.Elements<Cell>().Select(c =>
            c.DataType?.Value == CellValues.SharedString ? shared[int.Parse(c.CellValue!.Text)] : c.CellValue?.Text ?? c.InnerText).ToList()).ToList();
    }

    // ========================================================================= accesso alla pagina

    /// <summary>
    /// T01 — Apertura della pagina con utente ADMIN: tutte le chiamate che la pagina fa per mostrare
    /// filtri, griglia e stato rispondono senza errori.
    /// </summary>
    [Test]
    public async Task T01_AperturaPaginaAdmin_ShouldRispondereSuTutteLeSezioni()
    {
        var anni = await Get(_factory, InvioSapTestData.RottaAnni);
        var mesi = await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2003 }""");
        var tipologie = await Post(_factory, InvioSapTestData.RottaTipologie, """{ "anno": 2003 }""");
        var stati = await Get(_factory, InvioSapTestData.RottaStati);
        var griglia = await Griglia("""{ "anno": 2003 }""");
        var generazione = await Get(_factory, InvioSapTestData.RottaLock);

        Assert.Multiple(() =>
        {
            Assert.That(anni.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Anno");
            Assert.That(mesi.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Mese");
            Assert.That(tipologie.StatusCode, Is.EqualTo(HttpStatusCode.OK), "filtro Tipologia Fattura");
            Assert.That(stati.StatusCode, Is.EqualTo(HttpStatusCode.OK), "stati");
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.OK), "griglia");
            Assert.That(generazione.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), "nessuna generazione in corso: azioni abilitate");
        });
    }

    /// <summary>
    /// T02 — Un utente non ADMIN non accede: l'OPERATOR riceve 403 su ogni rotta della pagina, lettura
    /// compresa, e non riceve dati.
    /// </summary>
    [Test]
    public async Task T02_UtenteNonAdmin_ShouldRicevere403SenzaDati()
    {
        (HttpMethod Metodo, string Rotta, string? Body)[] rotte =
        [
            (HttpMethod.Get, InvioSapTestData.RottaAnni, null),
            (HttpMethod.Post, InvioSapTestData.RottaMesi, """{ "anno": 2003 }"""),
            (HttpMethod.Post, InvioSapTestData.RottaTipologie, """{ "anno": 2003 }"""),
            (HttpMethod.Get, InvioSapTestData.RottaStati, null),
            (HttpMethod.Post, InvioSapTestData.RottaGriglia, """{ "anno": 2003 }"""),
            (HttpMethod.Post, InvioSapTestData.RottaDettaglio, RigoInviate),
            (HttpMethod.Post, InvioSapTestData.RottaDownload, """{ "anno": 2003 }"""),
            (HttpMethod.Post, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003 }"""),
            (HttpMethod.Get, InvioSapTestData.RottaLock, null),
            (HttpMethod.Put, InvioSapTestData.RottaGenera, """{ "anno": 2003, "mese": 2, "tipologiaFattura": "PRIMO SALDO", "nota": "x" }""")
        ];

        foreach (var (metodo, rotta, body) in rotte)
        {
            var resp = await Invia(_factory, metodo, rotta, body, Ruolo.OPERATOR);
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"{metodo} {rotta}");
            Assert.That(await resp.Content.ReadAsStringAsync(), Is.Empty, $"{metodo} {rotta}: nessun dato esposto");
        }
        Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "nessuna generazione avviata");
    }

    /// <summary>
    /// T03 — Elementi della pagina coerenti: gli stati hanno codice e descrizione leggibile, e i righi
    /// della griglia hanno identificativi distinti (nessuna riga doppia).
    /// </summary>
    [Test]
    public async Task T03_ElementiDellaPagina_ShouldEssereCoerenti()
    {
        var stati = await Json(await Get(_factory, InvioSapTestData.RottaStati));
        var righe = await Json(await Griglia("""{ "anno": 2003 }"""));

        Assert.Multiple(() =>
        {
            Assert.That(stati.EnumerateArray().Select(s => s.GetProperty("descrizione").GetString()),
                Is.EqualTo(new[] { "Tutte", "Da inviare", "Inviate", "In elaborazione" }));
            Assert.That(Id(righe), Is.Unique.And.Count.EqualTo(8));
        });
    }

    // ===================================================================================== filtri

    /// <summary>
    /// T04 — Filtro Anno: l'elenco proposto contiene gli anni con fatture, dal più recente, senza
    /// ripetizioni. (Il valore di default "anno corrente" lo sceglie la pagina.) L'anno è obbligatorio
    /// per la griglia.
    /// </summary>
    [Test]
    public async Task T04_FiltroAnno_ShouldProporreGliAnniConFatture()
    {
        var anni = Interi(await Json(await Get(_factory, InvioSapTestData.RottaAnni)));
        var senzaAnno = await Griglia("{}");

        Assert.Multiple(async () =>
        {
            Assert.That(anni, Does.Contain(Anno).And.Unique.And.Ordered.Descending);
            Assert.That(senzaAnno.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await senzaAnno.Content.ReadAsStringAsync(), Does.Contain("Anno obbligatorio"));
        });
    }

    /// <summary>
    /// T05 — Mese = "Tutti": senza mesi (assenti, null o lista vuota) la griglia mostra tutti i mesi
    /// dell'anno.
    /// </summary>
    [TestCase("""{ "anno": 2003 }""")]
    [TestCase("""{ "anno": 2003, "mesi": [] }""")]
    [TestCase("""{ "anno": 2003, "mesi": null }""")]
    public async Task T05_MeseTutti_ShouldMostrareTuttiIMesiDellAnno(string body)
    {
        var righe = await Json(await Griglia(body));

        Assert.That(righe.EnumerateArray().Select(r => r.GetProperty("meseRiferimento").GetInt32()).Distinct(), Is.EquivalentTo(new[] { 1, 2, 3, 7 }));
    }

    /// <summary>
    /// T06 — Filtro Mese: propone i mesi con fatture dell'anno selezionato; cambiando anno l'elenco
    /// cambia, e coincide sempre con i mesi dei righi della griglia di quell'anno.
    /// </summary>
    [Test]
    public async Task T06_FiltroMese_ShouldSeguireLAnnoSelezionato()
    {
        var anni = Interi(await Json(await Get(_factory, InvioSapTestData.RottaAnni)));
        var altroAnno = anni.First(a => a != Anno);

        var mesi2003 = Interi(await Json(await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2003 }""")));
        var mesiAltro = Interi(await Json(await Post(_factory, InvioSapTestData.RottaMesi, $$"""{ "anno": {{altroAnno}} }""")));
        var grigliaAltro = await Json(await Griglia($$"""{ "anno": {{altroAnno}} }"""));

        Assert.Multiple(() =>
        {
            Assert.That(mesi2003, Is.EqualTo(new[] { 1, 2, 3, 7 }));
            Assert.That(mesiAltro, Is.EquivalentTo(grigliaAltro.EnumerateArray().Select(r => r.GetProperty("meseRiferimento").GetInt32()).Distinct()),
                $"anno {altroAnno}: i mesi proposti sono quelli della griglia");
        });
    }

    /// <summary>
    /// T07 — Filtro Tipologia Fattura: le tipologie disponibili nell'ordine del ciclo di fatturazione
    /// (Anticipo, Acconto, Primo Saldo, Secondo Saldo, Var. Semestrale…), e selezione multipla.
    /// </summary>
    [Test]
    public async Task T07_FiltroTipologiaFattura_ShouldEssereOrdinatoEMultiselezione()
    {
        var tipologie = Stringhe(await Json(await Post(_factory, InvioSapTestData.RottaTipologie, """{ "anno": 2003 }""")));
        var due = await Json(await Griglia("""{ "anno": 2003, "tipologieFattura": ["ANTICIPO", "SECONDO SALDO"] }"""));

        Assert.Multiple(() =>
        {
            Assert.That(tipologie, Is.EqualTo(new[] { "ANTICIPO", "ACCONTO", "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE" }));
            Assert.That(Id(due), Is.EqualTo(new[] { "ANTICIPO|2003|1|INVIATE", "SECONDO SALDO|2003|2|DA_INVIARE" }));
        });
    }

    /// <summary>
    /// T08 — Tipologia Invio "Tutte" / "Re-inviate": "Re-inviate" non è uno stato della griglia ma un
    /// download a parte, che contiene le sole fatture inviate più di una volta.
    /// </summary>
    [Test]
    public async Task T08_TipologiaInvioReInviate_ShouldEssereUnDownloadDedicato()
    {
        var stati = await Json(await Get(_factory, InvioSapTestData.RottaStati));
        var reinviate = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003 }""");

        Assert.Multiple(() =>
        {
            Assert.That(stati.EnumerateArray().Select(s => s.GetProperty("codice").GetString()), Has.None.Contains("REINVIAT"));
            Assert.That(reinviate.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    /// <summary>
    /// T13 — Download Fatture Re-inviate: il file contiene le sole fatture re-inviate coerenti con i
    /// filtri (9502, due invii riusciti); con un filtro che non ne contiene non viene prodotto un file.
    /// </summary>
    [Test]
    public async Task T13_DownloadFattureReInviate_ShouldContenereSoloLeReInviateDelFiltro()
    {
        var conFiltro = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003, "mesi": [1], "tipologieFattura": ["PRIMO SALDO"] }""");
        var altroMese = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003, "mesi": [2] }""");
        var righe = LeggiXlsx(await conFiltro.Content.ReadAsByteArrayAsync());

        Assert.Multiple(() =>
        {
            Assert.That(conFiltro.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(conFiltro.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Does.StartWith("InvioSAP_Reinviate_2003_").And.EndWith(".xlsx"));
            Assert.That(righe.Skip(1).Select(r => r[0]), Is.EqualTo(new[] { "9502" }), "9503 e 9501 sono state inviate una volta sola");
            Assert.That(altroMese.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "nessuna re-inviata a febbraio");
        });
    }

    /// <summary>
    /// T14 — Anno e Mese valorizzati e "Filtra": la griglia mostra solo i righi del periodo, e il
    /// download dei risultati contiene gli stessi righi.
    /// </summary>
    [Test]
    public async Task T14_FiltroAnnoEMese_ShouldMostrareSoloIRighiDelPeriodo()
    {
        const string filtro = """{ "anno": 2003, "mesi": [1] }""";
        var righe = await Json(await Griglia(filtro));
        var download = await Post(_factory, InvioSapTestData.RottaDownload, filtro);
        var foglio = LeggiXlsx(await download.Content.ReadAsByteArrayAsync());

        Assert.Multiple(() =>
        {
            Assert.That(Id(righe), Is.EqualTo(new[] { "ANTICIPO|2003|1|INVIATE", "PRIMO SALDO|2003|1|INVIATE" }));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK), "con risultati il download è disponibile");
            Assert.That(foglio.Count - 1, Is.EqualTo(righe.GetArrayLength()), "una riga del file per rigo della griglia");
        });
    }

    /// <summary>
    /// T15 — Filtro per ente: nella pagina l'ente si filtra nel Dettaglio Fatture (Ragione Sociale).
    /// Scelto un ente presente nel rigo, restano solo le sue fatture.
    /// </summary>
    [Test]
    public async Task T15_FiltroEnte_ShouldMostrareSoloLeFattureDellEnte()
    {
        var tutte = (await Json(await Dettaglio(RigoInviate))).GetProperty("fatture");
        var ente = tutte[1].GetProperty("ragioneSociale").GetString()!;

        var filtrate = await Json(await Dettaglio(RigoInviate.Replace(" }", $", \"ragioneSociale\": {JsonSerializer.Serialize(ente)} }}")));

        Assert.Multiple(() =>
        {
            Assert.That(filtrate.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("ragioneSociale").GetString()), Is.All.EqualTo(ente));
            Assert.That(filtrate.GetProperty("count").GetInt32(), Is.LessThan(tutte.GetArrayLength()), "il filtro restringe");
        });
    }

    /// <summary>
    /// T16 — Filtro Stato: per ogni stato la griglia restituisce solo righi in quello stato.
    /// </summary>
    [TestCase("DA_INVIARE", 3)]
    [TestCase("INVIATE", 4)]
    [TestCase("IN_ELABORAZIONE", 1)]
    public async Task T16_FiltroStato_ShouldRestituireSoloRighiInQuelloStato(string stato, int righi)
    {
        var righe = await Json(await Griglia($$"""{ "anno": 2003, "statoInvio": "{{stato}}" }"""));

        Assert.Multiple(() =>
        {
            Assert.That(righe.EnumerateArray().Select(r => r.GetProperty("statoInvio").GetString()), Is.All.EqualTo(stato));
            Assert.That(righe.GetArrayLength(), Is.EqualTo(righi));
        });
    }

    /// <summary>
    /// T17 — Periodo, tipologia e stato insieme: i criteri sono in AND e resta un solo rigo.
    /// </summary>
    [Test]
    public async Task T17_CombinazioneDiFiltri_ShouldApplicarliInAnd()
    {
        var righe = await Json(await Griglia("""{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "INVIATE" }"""));

        Assert.That(Id(righe), Is.EqualTo(new[] { "PRIMO SALDO|2003|2|INVIATE" }));
    }

    /// <summary>
    /// T19 — Combinazione senza fatture: nessun risultato (404) sulla griglia e nessun file dal
    /// download dei risultati.
    /// </summary>
    [Test]
    public async Task T19_FiltroSenzaRisultati_ShouldNonRestituireRighiNeFile()
    {
        const string filtro = """{ "anno": 2003, "mesi": [12], "statoInvio": "DA_INVIARE" }""";

        var griglia = await Griglia(filtro);
        var download = await Post(_factory, InvioSapTestData.RottaDownload, filtro);

        Assert.Multiple(async () =>
        {
            Assert.That(griglia.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(await griglia.Content.ReadAsStringAsync(), Is.Empty);
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "nessun file vuoto");
        });
    }

    // ==================================================================================== griglia

    /// <summary>
    /// T20 — Colonne della griglia: tipologia, anno, mese, stato, numero di fatture e importo, tutti
    /// valorizzati e coerenti fra loro.
    /// </summary>
    [Test]
    public async Task T20_ColonneDellaGriglia_ShouldEsserePresentiECoerenti()
    {
        var rigo = (await Json(await Griglia("""{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "DA_INVIARE" }"""))).EnumerateArray().Single();

        Assert.Multiple(() =>
        {
            Assert.That(rigo.GetProperty("id").GetString(), Is.EqualTo("PRIMO SALDO|2003|2|DA_INVIARE"));
            Assert.That(rigo.GetProperty("tipologiaFattura").GetString(), Is.EqualTo("PRIMO SALDO"));
            Assert.That(rigo.GetProperty("annoRiferimento").GetInt32(), Is.EqualTo(2003));
            Assert.That(rigo.GetProperty("meseRiferimento").GetInt32(), Is.EqualTo(2));
            Assert.That(rigo.GetProperty("statoInvio").GetString(), Is.EqualTo("DA_INVIARE"));
            Assert.That(rigo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(rigo.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(211.00m));
        });
    }

    /// <summary>
    /// T21 — Paginazione della griglia: la pagina fa lei la paginazione, quindi il backend restituisce
    /// tutti i righi del filtro in un'unica risposta; il totale è il numero di righi ricevuti.
    /// </summary>
    [Test]
    public async Task T21_PaginazioneDellaGriglia_ShouldRicevereTuttiIRighiDelFiltro()
    {
        var righe = await Json(await Griglia("""{ "anno": 2003 }"""));
        var nellaVista = Convert.ToInt32(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaGriglia} WHERE Anno = 2003"));

        Assert.That(righe.GetArrayLength(), Is.EqualTo(nellaVista).And.EqualTo(8));
    }

    // ========================================================================== dettaglio fatture

    /// <summary>
    /// T22 — Espansione di un rigo: l'elenco delle singole fatture con Ragione Sociale, ID Fattura,
    /// Data Fattura, Importo e File JSON.
    /// </summary>
    [Test]
    public async Task T22_EspansioneDelRigo_ShouldElencareLeFattureConIDati()
    {
        var corpo = await Json(await Dettaglio(RigoInviate));
        var fattura = corpo.GetProperty("fatture").EnumerateArray().Single(f => f.GetProperty("idFattura").GetInt64() == 9502);

        Assert.Multiple(() =>
        {
            Assert.That(IdFatture(corpo), Is.EqualTo(InvioSapTestData.FattureRigoInviate));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(fattura.GetProperty("ragioneSociale").GetString(), Is.Not.Empty, "Ragione Sociale");
            Assert.That(fattura.GetProperty("dataFattura").GetDateTime(), Is.EqualTo(new DateTime(2003, 2, 10)), "Data Fattura");
            Assert.That(fattura.GetProperty("importo").GetDecimal(), Is.GreaterThan(0), "Importo");
            Assert.That(fattura.GetProperty("nomeFile").GetString(), Is.EqualTo("FatturePagoPa_seed.json"), "File JSON");
            Assert.That(fattura.GetProperty("tipologiaFattura").GetString(), Is.EqualTo("PRIMO SALDO"));
        });
    }

    /// <summary>
    /// T23 — Filtri del Dettaglio Fatture: Ragione Sociale, ID Fattura e Data Fattura restringono
    /// l'elenco, e valgono solo dentro il rigo espanso: una fattura di un altro rigo non compare.
    /// </summary>
    [Test]
    public async Task T23_FiltriDelDettaglio_ShouldRestringereSoloIlRigoEspanso()
    {
        string Con(string campo) => RigoInviate.Replace(" }", $", {campo} }}");

        var perNumero = await Json(await Dettaglio(Con("\"numeroFattura\": \"9502\"")));
        var perData = await Json(await Dettaglio(Con("\"dataFattura\": \"2003-02-10\"")));
        var altraData = await Dettaglio(Con("\"dataFattura\": \"2003-02-11\""));
        var diUnAltroRigo = await Dettaglio(Con("\"numeroFattura\": \"9505\""));

        Assert.Multiple(() =>
        {
            Assert.That(IdFatture(perNumero), Is.EqualTo(new[] { 9502L }), "ID Fattura");
            Assert.That(IdFatture(perData), Is.EqualTo(InvioSapTestData.FattureRigoInviate), "Data Fattura");
            Assert.That(altraData.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "nessuna fattura in quella data");
            Assert.That(diUnAltroRigo.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "9505 è del rigo di febbraio");
        });
    }

    /// <summary>
    /// T25 — Il Dettaglio Fatture ha la propria paginazione, lato server: pagine diverse hanno fatture
    /// diverse e il totale è quello del rigo, non della pagina.
    /// </summary>
    [Test]
    public async Task T25_PaginazioneDelDettaglio_ShouldEssereLatoServer()
    {
        var prima = await Json(await Dettaglio(RigoInviate.Replace(" }", ", \"page\": 1, \"pageSize\": 1 }")));
        var seconda = await Json(await Dettaglio(RigoInviate.Replace(" }", ", \"page\": 2, \"pageSize\": 1 }")));

        Assert.Multiple(() =>
        {
            Assert.That(IdFatture(prima), Is.EqualTo(new[] { 9502L }));
            Assert.That(IdFatture(seconda), Is.EqualTo(new[] { 9503L }));
            Assert.That(prima.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(seconda.GetProperty("count").GetInt32(), Is.EqualTo(2));
        });
    }

    /// <summary>
    /// T33 — Date Invio: una fattura inviata ha l'elenco degli invii con data e nota (9502: tre);
    /// una fattura mai inviata non ne ha.
    /// </summary>
    [Test]
    public async Task T33_DateInvio_ShouldElencareGliInviiSoloPerLeFattureInviate()
    {
        var inviata = (await Json(await Dettaglio(RigoInviate))).GetProperty("fatture").EnumerateArray().Single(f => f.GetProperty("idFattura").GetInt64() == 9502);
        var maiInviate = (await Json(await Dettaglio(RigoDaInviare))).GetProperty("fatture").EnumerateArray().ToList();
        var storico = inviata.GetProperty("storicoInvii").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(inviata.GetProperty("numeroInvii").GetInt32(), Is.EqualTo(3));
            Assert.That(storico.Select(s => s.GetProperty("nInvio").GetInt32()), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(storico[0].GetProperty("testo").GetString(), Is.EqualTo("Migrazione seed"), "nota");
            Assert.That(storico.Select(s => s.GetProperty("data").ValueKind), Has.Some.EqualTo(JsonValueKind.String), "data e ora dell'invio");
            Assert.That(maiInviate.Select(f => f.GetProperty("numeroInvii").GetInt32()), Is.All.EqualTo(0));
            Assert.That(maiInviate.Select(f => f.GetProperty("storicoInvii").GetArrayLength()), Is.All.EqualTo(0));
        });
    }

    /// <summary>
    /// T34 — Nome del file JSON: valorizzato per le fatture inviate, assente per quelle da inviare
    /// (la pagina mostra "--").
    /// </summary>
    [Test]
    public async Task T34_NomeFileJson_ShouldEsserciSoloPerLeFattureInviate()
    {
        var inviate = (await Json(await Dettaglio(RigoInviate))).GetProperty("fatture").EnumerateArray().ToList();
        var daInviare = (await Json(await Dettaglio(RigoDaInviare))).GetProperty("fatture").EnumerateArray().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(inviate.Select(f => f.GetProperty("nomeFile").GetString()), Is.All.EndsWith(".json"));
            Assert.That(daInviare.Select(f => f.GetProperty("nomeFile").ValueKind), Is.All.EqualTo(JsonValueKind.Null));
            Assert.That(daInviare.Select(f => f.GetProperty("dataUltimaGenerazione").ValueKind), Is.All.EqualTo(JsonValueKind.Null));
        });
    }

    /// <summary>
    /// T35 — Date e importi: il backend non altera il dato sorgente. Gli importi delle fatture hanno
    /// al più due decimali e la loro somma è l'importo del rigo in griglia; le date sono in formato ISO
    /// (la presentazione italiana è della pagina).
    /// </summary>
    [Test]
    public async Task T35_DateEImporti_ShouldNonAlterareIlDatoSorgente()
    {
        var rigo = (await Json(await Griglia("""{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "DA_INVIARE" }"""))).EnumerateArray().Single();
        var fatture = (await Json(await Dettaglio(RigoDaInviare))).GetProperty("fatture").EnumerateArray().ToList();
        var importi = fatture.Select(f => f.GetProperty("importo").GetDecimal()).ToList();
        var aDb = InvioSapTestData.Lista<decimal>("SELECT CAST(TotaleFattura AS decimal(18,2)) FROM pfd.FattureTestata WHERE IdFattura IN (9505, 9506) ORDER BY IdFattura");

        Assert.Multiple(() =>
        {
            Assert.That(importi, Is.EqualTo(aDb), "importi uguali a quelli della fattura");
            Assert.That(importi.Select(i => decimal.Round(i, 2)), Is.EqualTo(importi), "al più due decimali");
            Assert.That(importi.Sum(), Is.EqualTo(rigo.GetProperty("importoTotale").GetDecimal()), "somma = importo del rigo");
            Assert.That(fatture.Select(f => f.GetProperty("dataFattura").GetString()), Is.All.Match(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}"));
        });
    }

    /// <summary>
    /// T36 — Ordinamento: quello per colonna è della pagina. Il backend restituisce righi e fatture in
    /// un ordine stabile, uguale a ogni chiamata, quindi il punto di partenza non cambia fra un
    /// aggiornamento e l'altro.
    /// </summary>
    [Test]
    public async Task T36_Ordinamento_ShouldPartireDaUnOrdineStabile()
    {
        var griglia1 = Id(await Json(await Griglia("""{ "anno": 2003 }""")));
        var griglia2 = Id(await Json(await Griglia("""{ "anno": 2003 }""")));
        var dettaglio1 = IdFatture(await Json(await Dettaglio(RigoInviate)));
        var dettaglio2 = IdFatture(await Json(await Dettaglio(RigoInviate)));

        Assert.Multiple(() =>
        {
            Assert.That(griglia2, Is.EqualTo(griglia1));
            Assert.That(griglia1.Select(id => int.Parse(id.Split('|')[2])), Is.Ordered, "righi in ordine di mese");
            Assert.That(dettaglio2, Is.EqualTo(dettaglio1), "fatture nello stesso ordine a ogni chiamata");
        });
    }

    // ============================================================= selezione e generazione del JSON

    /// <summary>
    /// T26 — Checkbox del rigo: seleziona tutte le fatture del rigo, e il numero delle fatture coinvolte
    /// è quello del rigo in griglia.
    /// </summary>
    [Test]
    public async Task T26_SelezioneDelRigoIntero_ShouldCoinvolgereTutteLeFattureDelRigo()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "Rigo intero" });
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2), "contatore degli elementi selezionati");
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(InvioSapTestData.FattureRigoDaInviare));
        });
    }

    /// <summary>
    /// T28 — Selezione di singole fatture dal Dettaglio: parte solo la fattura scelta, l'altra del
    /// rigo resta da inviare.
    /// </summary>
    [Test]
    public async Task T28_SelezioneDiSingoleFatture_ShouldInviareSoloQuelleScelte()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "Una sola" });
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(1));
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.Null, "scelta: in elaborazione");
            Assert.That(InvioSapTestData.FatturaInviata(9506), Is.False, "non scelta: resta da inviare");
        });
    }

    /// <summary>
    /// T30 — Rigo selezionato e una fattura deselezionata: il contatore scende e la fattura esclusa
    /// non parte.
    /// </summary>
    [Test]
    public async Task T30_RigoSelezionatoMenoUnaFattura_ShouldEscluderla()
    {
        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", allSelected = true, idFattureEscluse = new[] { 9503 }, nota = "Meno una", numeroDocumentoStorno = "NC-2003-0001" });
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(1));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(new[] { 9502L }));
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.True, "esclusa: non toccata");
        });
    }

    /// <summary>
    /// T31 — Un solo gruppo alla volta: una selezione con fatture di due righi diversi (tipologia,
    /// anno o mese) viene rifiutata, senza scrivere nulla.
    /// </summary>
    [Test]
    public async Task T31_FattureDiDueGruppi_ShouldEssereRifiutate()
    {
        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502, 9504 }, nota = "Due gruppi", numeroDocumentoStorno = "NC-1" });

        Assert.Multiple(async () =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("anno, al mese e alla tipologia"));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    /// <summary>
    /// T32 — Le fatture in elaborazione non sono selezionabili: né scegliendole dall'elenco né
    /// indicando lo stato del loro rigo parte una generazione.
    /// </summary>
    [Test]
    public async Task T32_FattureInElaborazione_ShouldNonEssereInviabili()
    {
        var dallElenco = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { InvioSapTestData.FatturaStessoMeseInElaborazione }, nota = "x" });
        var conLoStato = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "IN_ELABORAZIONE", allSelected = true, nota = "x" });

        Assert.Multiple(() =>
        {
            Assert.That(dallElenco.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(conLoStato.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaStessoMeseInElaborazione), Is.Null, "resta in elaborazione");
        });
    }

    /// <summary>
    /// T37 — Fattura con stato Inviata: è selezionabile, ma l'invio pretende Num. Documento Storno e
    /// motivazione (nota).
    /// </summary>
    [Test]
    public async Task T37_FatturaInviata_ShouldRichiedereStornoEMotivazione()
    {
        var senzaStorno = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, nota = "Motivo" });
        var senzaNota = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, numeroDocumentoStorno = "NC-1" });
        var completa = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, nota = "Motivo", numeroDocumentoStorno = "NC-1" });

        Assert.Multiple(async () =>
        {
            Assert.That(senzaStorno.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await senzaStorno.Content.ReadAsStringAsync(), Does.Contain("storno obbligatorio"));
            Assert.That(senzaNota.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(await senzaNota.Content.ReadAsStringAsync(), Does.Contain("nota e' obbligatoria"));
            Assert.That(completa.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        });
    }

    /// <summary>
    /// T40 — Conferma dell'invio di una fattura valida: parte una sola generazione, in stato di
    /// elaborazione; un secondo clic mentre è in corso viene respinto e non crea un secondo batch.
    /// </summary>
    [Test]
    public async Task T40_ConfermaInvio_ShouldAvviareUnaSolaGenerazione()
    {
        var body = new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "Conferma" };

        var primo = await Genera(body);
        var secondoClic = await Genera(body);
        var inCorso = await Get(_factory, InvioSapTestData.RottaLock);

        Assert.Multiple(async () =>
        {
            Assert.That(primo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That((await Json(primo)).GetProperty("stato").GetString(), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(secondoClic.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), "doppio clic");
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1), "un solo batch");
            Assert.That(FakeSynapseService.Instance.RichiesteGenerazione, Has.Count.EqualTo(1), "una sola richiesta a Synapse");
            Assert.That(inCorso.StatusCode, Is.EqualTo(HttpStatusCode.OK), "la pagina vede la generazione in corso");
        });
    }

    /// <summary>
    /// T41 — Conferma dell'invio di più fatture: un solo batch con una riga per fattura, senza duplicati.
    /// </summary>
    [Test]
    public async Task T41_InvioDiPiuFatture_ShouldCreareUnBatchSenzaDuplicati()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505, 9506 }, nota = "Due fatture" });
        var batch = (await Json(resp)).GetProperty("batchId").GetInt32();
        var righe = InvioSapTestData.Lista<long>($"SELECT FkIdFattura FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batch));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(righe, Is.EqualTo(new[] { 9505L, 9506L }));
            Assert.That(InvioSapTestData.Scalar($"SELECT CountFatture FROM {InvioSapTestData.Registro} WHERE BatchId = @b", ("@b", batch)), Is.EqualTo(2));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.EqualTo(new[] { batch }));
        });
    }

    /// <summary>
    /// T42, T43 — Il file JSON lo produce DATA: il backend passa alla pipeline il batch, il messaggio
    /// e il periodo da elaborare. Qui si verifica che la richiesta di generazione sia coerente con ciò
    /// che è stato scritto; il contenuto del JSON resta una verifica manuale sul file.
    /// </summary>
    [Test]
    public async Task T42_T43_RichiestaDiGenerazione_ShouldPortareBatchMessaggioEPeriodo()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "Per DATA" });
        var batch = (await Json(resp)).GetProperty("batchId").GetInt32();
        var richiesta = FakeSynapseService.Instance.RichiesteGenerazione.Single();

        Assert.Multiple(() =>
        {
            Assert.That(richiesta.BatchId, Is.EqualTo(batch));
            Assert.That(InvioSapTestData.Scalar("SELECT Hash FROM pfd.Messaggi WHERE IdMessaggio = @m", ("@m", richiesta.IdMessaggio)), Is.EqualTo(batch.ToString()), "il messaggio è quello del batch");
            Assert.That(richiesta.Parametri![0].MesiDaFatturare, Is.EqualTo(new[] { "200302" }), "periodo");
        });
    }

    /// <summary>
    /// T44 — Nota con accenti, apostrofi, virgolette e caratteri speciali: viene salvata così com'è e,
    /// a invio completato, compare identica fra le date di invio della fattura.
    /// </summary>
    [Test]
    public async Task T44_NotaConCaratteriSpeciali_ShouldEssereVisualizzataIdentica()
    {
        const string nota = "Perché l'ente ha chiesto \"rettifica\" — già così: 100% & <ok> | à è ì ò ù € ’";

        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota });
        InvioSapTestData.ChiudiBatchComeData((await Json(resp)).GetProperty("batchId").GetInt32());
        var inviate = await Json(await Dettaglio(RigoDaInviare.Replace("DA_INVIARE", "INVIATE")));
        var fattura = inviate.GetProperty("fatture").EnumerateArray().Single(f => f.GetProperty("idFattura").GetInt64() == 9505);

        Assert.That(fattura.GetProperty("storicoInvii").EnumerateArray().Last().GetProperty("testo").GetString(), Is.EqualTo(nota));
    }

    /// <summary>
    /// T46 — Batch completato e batch in errore: lo stato del batch è leggibile a DB con l'eventuale
    /// descrizione dell'errore, c'è il log dell'operazione, e le fatture finiscono inviate nel primo
    /// caso e di nuovo da inviare nel secondo.
    /// </summary>
    [TestCase("COMPLETATO", true)]
    [TestCase("ERRORE", false)]
    public async Task T46_EsitoDelBatch_ShouldEssereTracciato(string esito, bool inviate)
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "Esito " + esito });
        var batch = (await Json(resp)).GetProperty("batchId").GetInt32();

        InvioSapTestData.ChiudiBatchComeData(batch, esito);
        var rigoInviate = await Json(await Griglia("""{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "INVIATE" }"""));

        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.Scalar($"SELECT Stato FROM {InvioSapTestData.Registro} WHERE BatchId = @b", ("@b", batch)), Is.EqualTo(esito));
            Assert.That(InvioSapTestData.Scalar($"SELECT CASE WHEN DescrizioneErrore IS NULL THEN 0 ELSE 1 END FROM {InvioSapTestData.Registro} WHERE BatchId = @b", ("@b", batch)), Is.EqualTo(inviate ? 0 : 1));
            Assert.That(InvioSapTestData.ContaLogDeiTest(), Is.EqualTo(1), "log dell'operazione");
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.EqualTo(inviate));
            Assert.That(InvioSapTestData.FatturaInviata(9506), Is.EqualTo(inviate));
            Assert.That(rigoInviate.EnumerateArray().Single().GetProperty("numeroFatture").GetInt32(), Is.EqualTo(inviate ? 3 : 1));
            Assert.That(InvioSapTestData.BatchDelLock(), Is.Null, "generazione conclusa: lock rilasciato");
        });
    }

    /// <summary>
    /// T47 — Re-invio della stessa fattura dopo un batch completato: è permesso, ma solo con Numero
    /// Doc Storno e Nota.
    /// </summary>
    [Test]
    public async Task T47_ReInvioDopoBatchCompletato_ShouldRichiedereStornoENota()
    {
        var primo = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "Primo invio" });
        InvioSapTestData.ChiudiBatchComeData((await Json(primo)).GetProperty("batchId").GetInt32());

        var senzaStorno = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9505 }, nota = "Re-invio" });
        var reInvio = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9505 }, nota = "Re-invio", numeroDocumentoStorno = "NC-2003-0500" });
        var batch = (await Json(reInvio)).GetProperty("batchId").GetInt32();

        Assert.Multiple(() =>
        {
            Assert.That(senzaStorno.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(reInvio.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(InvioSapTestData.Scalar($"SELECT CONCAT(FkStatoPrecedente, '>', FkStato, ':', NumeroDocStorno) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batch)),
                Is.EqualTo("5>5:NC-2003-0500"), "da inviata a inviata, con lo storno");
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(2));
        });
    }

    /// <summary>
    /// T48 — Dopo un invio riuscito, ricaricando la pagina: il rigo "da inviare" non c'è più, le
    /// fatture sono fra le inviate con data di generazione e nome del file aggiornati.
    /// </summary>
    [Test]
    public async Task T48_DopoInvioRiuscito_ShouldAggiornareStatoEData()
    {
        var resp = await Genera(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", allSelected = true, nota = "Invio riuscito" });
        InvioSapTestData.ChiudiBatchComeData((await Json(resp)).GetProperty("batchId").GetInt32());

        var daInviare = await Griglia("""{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "DA_INVIARE" }""");
        var inviate = await Json(await Dettaglio(RigoDaInviare.Replace("DA_INVIARE", "INVIATE")));
        var f9505 = inviate.GetProperty("fatture").EnumerateArray().Single(f => f.GetProperty("idFattura").GetInt64() == 9505);

        Assert.Multiple(() =>
        {
            Assert.That(daInviare.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "il rigo da inviare è vuoto");
            Assert.That(IdFatture(inviate), Is.EquivalentTo(new[] { 9505L, 9506L, 9507L }));
            Assert.That(f9505.GetProperty("statoInvio").GetString(), Is.EqualTo("INVIATE"));
            Assert.That(f9505.GetProperty("nomeFile").GetString(), Is.EqualTo("FatturePagoPa_test.json"));
            Assert.That(f9505.GetProperty("dataUltimaGenerazione").ValueKind, Is.EqualTo(JsonValueKind.String), "data e ora dell'invio");
            Assert.That(f9505.GetProperty("numeroInvii").GetInt32(), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// T49 — Stringhe HTML/JS e caratteri di controllo in nota e storno: sono trattate come dati. Il
    /// JSON della nota a DB resta valido e i valori tornano identici, senza essere interpretati.
    /// </summary>
    [Test]
    public async Task T49_HtmlEScriptNeiCampi_ShouldEssereTrattatiComeDati()
    {
        const string nota = "<script>alert('xss')</script><img src=x onerror=alert(1)> \t tab \n a capo \\ \" } ]";
        const string storno = "<b>NC</b>\"';--";

        var resp = await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9503 }, nota, numeroDocumentoStorno = storno });
        var batch = (await Json(resp)).GetProperty("batchId").GetInt32();
        InvioSapTestData.ChiudiBatchComeData(batch);
        var fattura = (await Json(await Dettaglio(RigoInviate))).GetProperty("fatture").EnumerateArray().Single(f => f.GetProperty("idFattura").GetInt64() == 9503);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(InvioSapTestData.Scalar($"SELECT ISJSON(CAST(Nota AS nvarchar(max))) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batch)), Is.EqualTo(1), "JSON della nota valido");
            Assert.That(InvioSapTestData.Scalar($"SELECT NumeroDocStorno FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batch)), Is.EqualTo(storno));
            Assert.That(fattura.GetProperty("storicoInvii").EnumerateArray().Last().GetProperty("testo").GetString(), Is.EqualTo(nota));
        });
    }

    /// <summary>
    /// T50 — Due sessioni ADMIN inviano insieme la stessa fattura: una sola generazione è accettata,
    /// l'altra riceve il conflitto.
    /// </summary>
    [Test]
    public async Task T50_DueSessioniInsieme_ShouldAccettarneUnaSola()
    {
        InvioSapTestData.RilasciaLockComeData();
        var body = JsonSerializer.Serialize(new { anno = 2003, mese = 2, tipologiaFattura = "PRIMO SALDO", statoInvio = "DA_INVIARE", idFatture = new[] { 9505 }, nota = "Due sessioni" }, Web);

        var esiti = await Task.WhenAll(
            Put(_factory, InvioSapTestData.RottaGenera, body),
            Put(_factory, InvioSapTestData.RottaGenera, body));
        var conflitto = esiti.Single(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Multiple(async () =>
        {
            Assert.That(esiti.Select(r => r.StatusCode), Is.EquivalentTo(new[] { HttpStatusCode.Accepted, HttpStatusCode.Conflict }));
            Assert.That((await Json(conflitto)).GetProperty("motivo").GetString(), Does.StartWith("LOCK_"), "conflitto di generazione");
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkIdFattura = 9505 AND FkBatchId > {InvioSapTestData.UltimoBatchSeed}"), Is.EqualTo(1), "la fattura è in un solo batch");
        });
    }

    /// <summary>
    /// T51 — Messaggi di errore: quelli che il backend manda alla pagina sono frasi in italiano, non
    /// tracce tecniche.
    /// </summary>
    [Test]
    public async Task T51_MessaggiDiErrore_ShouldEssereComprensibili()
    {
        var senzaAnno = await (await Griglia("{}")).Content.ReadAsStringAsync();
        var senzaStorno = await (await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", idFatture = new[] { 9502 }, nota = "x" })).Content.ReadAsStringAsync();
        var senzaSelezione = await (await Genera(new { anno = 2003, mese = 1, tipologiaFattura = "PRIMO SALDO", statoInvio = "INVIATE", nota = "x", numeroDocumentoStorno = "NC-1" })).Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(senzaAnno, Does.Contain("Anno obbligatorio"));
            Assert.That(senzaStorno, Does.Contain("Numero documento di storno obbligatorio per il re-invio"));
            Assert.That(senzaSelezione, Does.Contain("Selezionare almeno una fattura"));
            foreach (var messaggio in new[] { senzaAnno, senzaStorno, senzaSelezione })
                Assert.That(messaggio, Does.Not.Contain("Exception").And.Not.Contain("   at ").And.Not.Contain("System."));
        });
    }
}
