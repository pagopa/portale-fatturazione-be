using System.Net;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte di lettura dell'Invio a SAP sul seed 2003, attraverso la pipeline reale (be-invio-sap.md §3.1-3.8):
/// griglia con i quattro stati, dettaglio solo per i righi con storico, download, lock e stati.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapLetturaHttpTests
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
    public void Reset() => InvioSapTestData.Pulisci();

    private static List<string> Id(JsonElement righe) => righe.EnumerateArray().Select(r => r.GetProperty("id").GetString()!).ToList();

    // =============================================================================================
    // Griglia
    // =============================================================================================

    [Test]
    public async Task Griglia_Anno_ShouldRestituireUnRigoPerStato_InOrdine()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, """{ "anno": 2003 }""");
        var righe = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(Id(righe), Is.EqualTo(new[]
            {
                "ANTICIPO|2003|1|INVIATE", "PRIMO SALDO|2003|1|INVIATE",
                "PRIMO SALDO|2003|2|DA_INVIARE", "PRIMO SALDO|2003|2|INVIATE", "PRIMO SALDO|2003|2|IN_ELABORAZIONE",
                "SECONDO SALDO|2003|2|DA_INVIARE", "ACCONTO|2003|3|DA_INVIARE", "VAR. SEMESTRALE|2003|7|INVIATE"
            }));
        });
    }

    [TestCase("DA_INVIARE", 3, TestName = "Griglia_FiltroStato_ShouldRestringere(Griglia: DA_INVIARE = 0)")]
    [TestCase("INVIATE", 4, TestName = "Griglia_FiltroStato_ShouldRestringere(Griglia: INVIATE = 1)")]
    [TestCase("IN_ELABORAZIONE", 1, TestName = "Griglia_FiltroStato_ShouldRestringere(Griglia: IN_ELABORAZIONE = NULL)")]
    [TestCase("TUTTE", 8, TestName = "Griglia_FiltroStato_ShouldRestringere(Griglia: TUTTE)")]
    [TestCase(" inviate ", 4, TestName = "Griglia_FiltroStato_ShouldRestringere(Griglia: stato minuscolo con spazi)")]
    public async Task Griglia_FiltroStato_ShouldRestringere(string stato, int righi)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, $$"""{ "anno": 2003, "statoInvio": "{{stato}}" }""");
        var righe = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(righe.GetArrayLength(), Is.EqualTo(righi));
            if (stato.Trim() != "TUTTE")
                Assert.That(righe.EnumerateArray().Select(r => r.GetProperty("statoInvio").GetString()).Distinct(),
                    Is.EqualTo(new[] { stato.Trim().ToUpperInvariant() }));
        });
    }

    [Test]
    public async Task Griglia_MesiETipologie_ShouldRestringere_ConImportoArrotondato()
    {
        var righe = await Json(await Post(_factory, InvioSapTestData.RottaGriglia,
            """{ "anno": 2003, "mesi": [2], "tipologieFattura": ["PRIMO SALDO"], "statoInvio": "DA_INVIARE" }"""));
        var rigo = righe.EnumerateArray().Single();
        Assert.Multiple(() =>
        {
            Assert.That(rigo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(rigo.GetProperty("importoTotale").GetDecimal(), Is.EqualTo(211.00m));
            Assert.That(rigo.GetProperty("annoRiferimento").GetInt32(), Is.EqualTo(2003));
            Assert.That(rigo.GetProperty("meseRiferimento").GetInt32(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Griglia_SenzaRighi_ShouldEssere404()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaGriglia, """{ "anno": 2003, "mesi": [12] }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // =============================================================================================
    // Dettaglio
    // =============================================================================================

    private const string DettaglioInviate = """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" }""";

    [Test]
    public async Task Dettaglio_RigoInviate_ShouldRestituireLeFattureConLoStorico()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDettaglio, DettaglioInviate);
        var corpo = await Json(resp);
        var fatture = corpo.GetProperty("fatture").EnumerateArray().ToList();
        var f9502 = fatture.Single(f => f.GetProperty("idFattura").GetInt64() == 9502);
        var f9503 = fatture.Single(f => f.GetProperty("idFattura").GetInt64() == 9503);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(f9502.GetProperty("numeroInvii").GetInt32(), Is.EqualTo(3), "C13: anche la nota del batch in errore.");
            Assert.That(f9502.GetProperty("storicoInvii")[0].GetProperty("testo").GetString(), Is.EqualTo("Migrazione seed"));
            Assert.That(f9502.GetProperty("storicoInvii")[2].GetProperty("nInvio").GetInt32(), Is.EqualTo(3));
            Assert.That(f9502.GetProperty("nomeFile").GetString(), Is.EqualTo("FatturePagoPa_seed.json"));
            Assert.That(f9502.GetProperty("statoInvio").GetString(), Is.EqualTo("INVIATE"));
            Assert.That(f9503.GetProperty("idDocumento").ValueKind, Is.EqualTo(JsonValueKind.Null), "C11.");
            Assert.That(f9503.GetProperty("tipoDocumento").GetString(), Is.EqualTo("TD04"));
        });
    }

    [Test]
    public async Task Dettaglio_RigoDaInviare_ShouldRestituireLeFattureSenzaStorico()
    {
        // Dall'11/11/2026 (vista 1.1): il rigo da inviare ha il dettaglio, per poter selezionare le fatture.
        var resp = await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 2, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "DA_INVIARE" }""");
        var corpo = await Json(resp);
        var fatture = corpo.GetProperty("fatture").EnumerateArray().ToList();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(fatture.Select(f => f.GetProperty("idFattura").GetInt64()), Is.EqualTo(new[] { 9505L, 9506L }));
            Assert.That(fatture.Select(f => f.GetProperty("statoInvio").GetString()), Is.All.EqualTo("DA_INVIARE"));
            Assert.That(fatture.Select(f => f.GetProperty("numeroInvii").GetInt32()), Is.All.EqualTo(0), "Mai inviate: nessuno storico.");
            Assert.That(fatture.Select(f => f.GetProperty("nomeFile").ValueKind), Is.All.EqualTo(JsonValueKind.Null));
        });
    }

    [Test]
    public async Task Dettaglio_RigoInElaborazione_ShouldMostrareAncheLeFattureSenzaStorico()
    {
        // 9508 e' NULL e nel seed non ha batch: dalla vista 1.1 (11/11/2026) compare lo stesso, senza file ne' data.
        var corpo = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 2, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "IN_ELABORAZIONE" }"""));
        var ids = corpo.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("idFattura").GetInt64()).ToList();
        Assert.That(ids, Is.EqualTo(new[] { 9508L }));
    }

    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "ragioneSociale": "test 2" }""", 9503L, TestName = "Dettaglio_Filtri_ShouldRestringere_ConCountCoerente(Dettaglio: ragione sociale, case insensitive)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "numeroFattura": "9502" }""", 9502L, TestName = "Dettaglio_Filtri_ShouldRestringere_ConCountCoerente(Dettaglio: numero fattura)")]
    [TestCase("""{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "page": 2, "pageSize": 1 }""", 9503L, TestName = "Dettaglio_Filtri_ShouldRestringere_ConCountCoerente(Dettaglio: seconda pagina)")]
    public async Task Dettaglio_Filtri_ShouldRestringere_ConCountCoerente(string body, long atteso)
    {
        var corpo = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio, body));
        var ids = corpo.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("idFattura").GetInt64()).ToList();
        Assert.That(ids, Is.EqualTo(new[] { atteso }));
        if (!body.Contains("page"))
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(1));
        else
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2), "Il count e' quello totale, non della pagina.");
    }

    [Test]
    public async Task Dettaglio_DataFattura_ShouldFiltrareSulGiorno()
    {
        var corpo = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE", "dataFattura": "2003-02-10" }"""));
        Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(2));
    }

    // =============================================================================================
    // Download
    // =============================================================================================

    private static List<List<string>> LeggiXlsx(byte[] contenuto)
    {
        using var ms = new MemoryStream(contenuto);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var wb = doc.WorkbookPart!;
        var shared = wb.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().Select(s => s.InnerText).ToList() ?? [];
        var sheet = wb.WorksheetParts.First().Worksheet;
        return sheet.Descendants<Row>().Select(r => r.Elements<Cell>().Select(c =>
            c.DataType?.Value == CellValues.SharedString ? shared[int.Parse(c.CellValue!.Text)] : c.CellValue?.Text ?? c.InnerText).ToList()).ToList();
    }

    [Test]
    public async Task Download_ShouldEssereUnXlsx_ConUnaRigaPerRigo()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDownload, """{ "anno": 2003, "mesi": [2] }""");
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var righe = LeggiXlsx(bytes);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(bytes.Take(2), Is.EqualTo("PK"u8.ToArray()), "Firma zip di un xlsx vero.");
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Does.StartWith("InvioSAP_Risultati_2003_"));
            Assert.That(righe[0], Is.EqualTo(new[] { "Tipologia Fattura", "Anno", "Mese", "Stato", "Numero Fatture", "Importo Totale" }));
            Assert.That(righe.Count - 1, Is.EqualTo(4), "Quattro righi nel mese 2.");
        });
    }

    [Test]
    public async Task DownloadReinviate_ShouldContenereSolo9502()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003 }""");
        var righe = LeggiXlsx(await resp.Content.ReadAsByteArrayAsync());
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(resp.Content.Headers.ContentDisposition?.FileName?.Trim('"'), Does.StartWith("InvioSAP_Reinviate_2003_"));
            Assert.That(righe.Count, Is.EqualTo(2), "Intestazione + 9502 (C3: 9501 migrata una volta non e' re-inviata).");
            Assert.That(righe[1][0], Is.EqualTo("9502"));
            Assert.That(righe[1][10], Is.EqualTo("2"), "ConteggioInvio.");
        });
    }

    [Test]
    public async Task DownloadReinviate_SenzaRighe_ShouldEssere404_NonUnFileVuoto()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaDownloadReinviate, """{ "anno": 2003, "mesi": [7] }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // =============================================================================================
    // Lock e stati
    // =============================================================================================

    [Test]
    public async Task Lock_ConRigaScadutaDelSeed_ShouldEssere204()
    {
        var resp = await Get(_factory, InvioSapTestData.RottaLock);
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), "C9: la vista non mostra un lock scaduto.");
    }

    [Test]
    public async Task Lock_Attivo_ShouldEssere200_InOraItaliana()
    {
        InvioSapTestData.AttivaLock(InvioSapTestData.BatchErrore);
        var resp = await Get(_factory, InvioSapTestData.RottaLock);
        var corpo = await Json(resp);
        var utc = (DateTime)InvioSapTestData.Scalar($"SELECT lockTimeout FROM {InvioSapTestData.Lock}")!;
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(corpo.GetProperty("batchId").GetInt32(), Is.EqualTo(InvioSapTestData.BatchErrore));
            Assert.That(corpo.GetProperty("azione").GetString(), Is.EqualTo("GeneraJsonFatture"));
            var italiana = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome"));
            Assert.That(corpo.GetProperty("lockTimeout").GetDateTime(), Is.EqualTo(italiana).Within(TimeSpan.FromSeconds(1)));
        });
    }

    // =============================================================================================
    // Filtri Anno e Mese: i valori presenti in griglia (11/11/2026)
    // =============================================================================================

    private static List<int> Interi(JsonElement lista) => lista.EnumerateArray().Select(e => e.GetInt32()).ToList();

    [Test]
    public async Task Anni_ShouldEssereGliAnniDellaGriglia_DistintiDalPiuRecente()
    {
        var attesi = InvioSapTestData.Lista<int>($"SELECT DISTINCT Anno FROM {InvioSapTestData.VistaGriglia} ORDER BY Anno DESC");

        var resp = await Get(_factory, InvioSapTestData.RottaAnni);
        var anni = Interi(await Json(resp));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(anni, Is.EqualTo(attesi), "Stessi anni della vista griglia, nello stesso ordine.");
            Assert.That(anni, Does.Contain(InvioSapTestData.Anno), "Il seed dell'Invio a SAP sta sul 2003.");
            Assert.That(anni, Is.Unique);
            Assert.That(anni, Is.Ordered.Descending);
            Assert.That(anni.Count, Is.GreaterThan(1), "La griglia legge tutta pfd.FattureTestata: nel seed ci sono piu' anni.");
        });
    }

    [Test]
    public async Task Mesi_ShouldEssereIMesiDellaGrigliaPerLAnno_DistintiInOrdine()
    {
        var resp = await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2003 }""");
        var mesi = Interi(await Json(resp));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(mesi, Is.EqualTo(new[] { 1, 2, 3, 7 }),
                "Seed 2003: mesi 1, 2 (tre righi, un solo valore), 3 e 7. Nessun mese senza fatture.");
        });
    }

    [Test]
    public async Task Mesi_ShouldCoincidereConIMesiDeiRighiDellaGriglia()
    {
        // E' la promessa fatta al FE: ogni mese del filtro ha almeno un rigo in griglia, e viceversa.
        var griglia = await Json(await Post(_factory, InvioSapTestData.RottaGriglia, """{ "anno": 2003 }"""));
        var mesiInGriglia = griglia.EnumerateArray().Select(r => r.GetProperty("meseRiferimento").GetInt32()).Distinct().OrderBy(m => m).ToList();

        var mesi = Interi(await Json(await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2003 }""")));

        Assert.That(mesi, Is.EqualTo(mesiInGriglia));
    }

    [Test]
    public async Task Mesi_AnnoSenzaFatture_ShouldEssere404()
    {
        // Come la griglia: senza valori la risposta e' 404 senza corpo (contratto atteso dal FE).
        Assume.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaGriglia} WHERE Anno = 2001"), Is.EqualTo(0));
        var resp = await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2001 }""");
        var corpo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(corpo, Is.Empty);
        });
    }

    // ---- Filtro Tipologia (11/11/2026): tipologie presenti in griglia per anno e mesi, nell'ordine del ciclo ----

    private static List<string> Stringhe(JsonElement lista) => lista.EnumerateArray().Select(e => e.GetString()!).ToList();

    [TestCase("""{ "anno": 2003 }""", new[] { "ANTICIPO", "ACCONTO", "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE" },
        TestName = "Tipologie_ShouldEssereQuelleDellaGriglia_NellOrdineDelCiclo(tutto l'anno: ordine del ciclo, non alfabetico)")]
    [TestCase("""{ "anno": 2003, "mesi": [2] }""", new[] { "PRIMO SALDO", "SECONDO SALDO" },
        TestName = "Tipologie_ShouldEssereQuelleDellaGriglia_NellOrdineDelCiclo(febbraio: PRIMO SALDO su tre stati compare una volta)")]
    [TestCase("""{ "anno": 2003, "mesi": [3, 1] }""", new[] { "ANTICIPO", "ACCONTO", "PRIMO SALDO" },
        TestName = "Tipologie_ShouldEssereQuelleDellaGriglia_NellOrdineDelCiclo(mesi fuori ordine: unione dei due mesi)")]
    [TestCase("""{ "anno": 2003, "mesi": [] }""", new[] { "ANTICIPO", "ACCONTO", "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE" },
        TestName = "Tipologie_ShouldEssereQuelleDellaGriglia_NellOrdineDelCiclo(mesi vuoti: come assenti)")]
    public async Task Tipologie_ShouldEssereQuelleDellaGriglia_NellOrdineDelCiclo(string body, string[] attese)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaTipologie, body);
        var tipologie = Stringhe(await Json(resp));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(tipologie, Is.EqualTo(attese));
        });
    }

    [Test]
    public async Task Tipologie_ShouldCoincidereConLeTipologieDeiRighiDellaGriglia()
    {
        // La stessa promessa del filtro Mese: ogni tipologia proposta ha almeno un rigo, e viceversa, nello stesso ordine.
        const string body = """{ "anno": 2003, "mesi": [1, 2] }""";
        var griglia = await Json(await Post(_factory, InvioSapTestData.RottaGriglia, body));
        var inGriglia = griglia.EnumerateArray().Select(r => r.GetProperty("tipologiaFattura").GetString()!).Distinct().ToList();

        var tipologie = Stringhe(await Json(await Post(_factory, InvioSapTestData.RottaTipologie, body)));

        Assert.That(tipologie, Is.EquivalentTo(inGriglia));
    }

    [TestCase("""{ "anno": 2001 }""", TestName = "Tipologie_SenzaFatture_ShouldEssere404(anno senza fatture)")]
    [TestCase("""{ "anno": 2003, "mesi": [5] }""", TestName = "Tipologie_SenzaFatture_ShouldEssere404(mese senza fatture in un anno che ne ha)")]
    public async Task Tipologie_SenzaFatture_ShouldEssere404(string body)
    {
        var resp = await Post(_factory, InvioSapTestData.RottaTipologie, body);
        var corpo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(corpo, Is.Empty);
        });
    }

    [Test]
    public async Task AnniEMesi_ShouldSeguireLaGriglia_QuandoArrivaUnaFatturaNuova()
    {
        // Anno 2002, libero nel seed: prima non c'e', dopo l'inserimento compare con il suo mese.
        const long id = 9690;
        Assume.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaGriglia} WHERE Anno = 2002"), Is.EqualTo(0));
        try
        {
            var prima = Interi(await Json(await Get(_factory, InvioSapTestData.RottaAnni)));
            InvioSapTestData.Esegui(@"SET IDENTITY_INSERT pfd.FattureTestata ON;
                INSERT INTO pfd.FattureTestata (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
                    FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento, Progressivo)
                SELECT @id, FkIdEnte, FkTipologiaFattura, 2002, 11, 0, FkProdotto, FkIdTipoDocumento, '2002-12-10', 'IT-9690',
                       TotaleFattura, Divisa, MetodoPagamento, @id
                  FROM pfd.FattureTestata WHERE IdFattura = 9505;
                SET IDENTITY_INSERT pfd.FattureTestata OFF;", ("@id", id));

            var dopo = Interi(await Json(await Get(_factory, InvioSapTestData.RottaAnni)));
            var mesi = Interi(await Json(await Post(_factory, InvioSapTestData.RottaMesi, """{ "anno": 2002 }""")));

            Assert.Multiple(() =>
            {
                Assert.That(prima, Does.Not.Contain(2002));
                Assert.That(dopo, Does.Contain(2002));
                Assert.That(dopo, Is.Ordered.Descending);
                Assert.That(mesi, Is.EqualTo(new[] { 11 }));
            });
        }
        finally
        {
            InvioSapTestData.Esegui("DELETE FROM pfd.FattureTestata WHERE IdFattura = @id", ("@id", id));
        }
    }

    [Test]
    public async Task Stati_ShouldEssereIQuattroCodici()
    {
        var corpo = await Json(await Get(_factory, InvioSapTestData.RottaStati));
        Assert.That(corpo.EnumerateArray().Select(s => s.GetProperty("codice").GetString()),
            Is.EqualTo(new[] { "TUTTE", "DA_INVIARE", "INVIATE", "IN_ELABORAZIONE" }));
    }

    /// <summary>
    /// Testbook PF-965 T20 (etichette degli stati). Fissa le descrizioni che il FE mostra nei chip e nel
    /// filtro. ⚠️ Il testbook dei colleghi (06/11/2026) si aspetta anche "Re-inviata", che nel contratto
    /// v2.2 non e' uno stato ma il download separato delle re-inviate: se si decide di aggiungerlo, questo
    /// test va aggiornato insieme alla regola, non "aggiustato".
    /// </summary>
    [Test]
    public async Task Stati_ShouldAvereLeDescrizioniMostrateDalFe()
    {
        var corpo = await Json(await Get(_factory, InvioSapTestData.RottaStati));
        Assert.That(corpo.EnumerateArray().Select(s => s.GetProperty("descrizione").GetString()),
            Is.EqualTo(new[] { "Tutte", "Da inviare", "Inviate", "In elaborazione" }));
    }

    /// <summary>
    /// Testbook PF-965 T07: le tipologie si presentano nell'ordine del ciclo di fatturazione (Anticipo,
    /// Acconto, Primo Saldo, Secondo Saldo, Var. Semestrale, Sem. Sospesi; poi Var. Annuale), non in
    /// ordine alfabetico. Le fatture 9601-9607 sull'anno 2004, libero, sono inserite apposta in ordine
    /// INVERSO: un ORDER BY alfabetico o per IdFattura darebbe un ordine diverso.
    /// </summary>
    [Test]
    public async Task Griglia_StessoMese_ShouldOrdinareLeTipologieSecondoIlCiclo()
    {
        string[] attese = ["ANTICIPO", "ACCONTO", "PRIMO SALDO", "SECONDO SALDO", "VAR. SEMESTRALE", "SEM. SOSPESI", "VAR. ANNUALE"];
        try
        {
            for (var i = attese.Length - 1; i >= 0; i--)
                InvioSapTestData.Esegui(@"
                    SET IDENTITY_INSERT pfd.FattureTestata ON; -- IdFattura e' identity: come nel seed
                    INSERT INTO pfd.FattureTestata
                     (IdFattura, FkIdEnte, FkTipologiaFattura, AnnoRiferimento, MeseRiferimento, FatturaInviata,
                      FkProdotto, FkIdTipoDocumento, DataFattura, IdentificativoFattura, TotaleFattura, Divisa, MetodoPagamento,
                      Progressivo, IdDocumento, Semestre)
                    VALUES (@id, '11111111-1111-1111-1111-111111111111', @t, 2004, 1, 0,
                      'prod-pn', 'TD01', '2004-02-10', CONCAT('IT-', @id), 1.00, 'EUR', 'MP5', @id, NULL, NULL);
                    SET IDENTITY_INSERT pfd.FattureTestata OFF;",
                    ("@id", 9601 + i), ("@t", attese[i]));

            var righe = await Json(await Post(_factory, InvioSapTestData.RottaGriglia, """{ "anno": 2004 }"""));
            Assert.That(righe.EnumerateArray().Select(r => r.GetProperty("tipologiaFattura").GetString()), Is.EqualTo(attese));
        }
        finally
        {
            InvioSapTestData.Esegui("DELETE FROM pfd.FattureTestata WHERE IdFattura BETWEEN 9601 AND 9607 AND AnnoRiferimento = 2004");
        }
    }
}
