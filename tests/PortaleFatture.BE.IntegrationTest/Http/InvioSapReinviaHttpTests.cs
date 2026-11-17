using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// End-to-end di <c>PUT api/fatture/invio-sap/reinvia</c> sul seed 2003: pipeline reale fino al DB, Synapse finto.
/// Il re-invio scrive lo stesso batch del Genera (stesse tabelle, stesso ordine, createRun dopo il commit), ma con
/// nota e documento di storno <b>per fattura</b>, passati alla scrittura come un solo parametro JSON (<c>OPENJSON</c>).
/// Rigo del seed: 2003/01 PRIMO SALDO, fatture inviate 9502 e 9503. Ogni test parte dallo stato del seed e lo ripristina.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapReinviaHttpTests
{
    private ApiTestFactory _factory = null!;
    private const int Anno = InvioSapTestData.Anno;
    private const int Mese = InvioSapTestData.MeseReInvio;
    private const string Tipologia = InvioSapTestData.TipologiaReInvio;

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
        FakeSynapseService.Instance.Reset();
        _factory?.Dispose();
    }

    [SetUp]
    public void Reset()
    {
        FakeSynapseService.Instance.Reset();
        InvioSapTestData.Pulisci();
    }

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    private sealed record F(long? idFattura, string? nota, string? numeroDocumentoStorno);

    private static string Body(params F[] fatture) => Body(Anno, Mese, Tipologia, fatture);

    private static string Body(int anno, int mese, string tipologia, params F[] fatture) =>
        JsonSerializer.Serialize(new { anno, mese, tipologiaFattura = tipologia, fatture }, Web);

    private static readonly F[] DueFatture =
    [
        new(9502, "Nota della 9502", "NC-2003-0102"),
        new(9503, "Nota della 9503", "NC-2003-0103")
    ];

    private Task<HttpResponseMessage> Reinvia(string body) => Put(_factory, InvioSapTestData.RottaReinvia, body);

    private static void NessunaScrittura(InvioSapTestData.Fotografia prima, string perche)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, $"Nessun batch: {perche}");
            Assert.That(InvioSapTestData.ContaLogDeiTest(), Is.EqualTo(0), $"Nessun log: {perche}");
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.Messaggi WHERE IdMessaggio > @m", ("@m", prima.MaxMessaggio)), Is.EqualTo(0), $"Nessun messaggio: {perche}");
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima), $"Righe preesistenti invariate: {perche}");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, $"Nessuna pipeline: {perche}");
        });
    }

    private static List<string> RigheDettaglio(int batchId) => InvioSapTestData.Lista<string>($@"
        SELECT CONCAT(FkIdFattura, ':', FkStatoPrecedente, '>', FkStato, ':', NumeroDocStorno, ':', JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Testo'))
          FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));

    // =============================================================================================
    // Caso felice: nota e storno per fattura
    // =============================================================================================

    [Test]
    public async Task Reinvio_ShouldScrivereNotaEStornoDiOgniFattura_EMetterleInElaborazione()
    {
        InvioSapTestData.RilasciaLockComeData();
        var prima = InvioSapTestData.FotografiaTabelle(fattureDelBatch: InvioSapTestData.FattureRigoInviate);

        var resp = await Reinvia(Body(DueFatture));
        var corpo = await Json(resp);
        var batchId = corpo.GetProperty("batchId").GetInt32();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("stato").GetString(), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(InvioSapTestData.FattureRigoInviate));
            Assert.That(RigheDettaglio(batchId), Is.EqualTo(new[]
            {
                "9502:5>5:NC-2003-0102:Nota della 9502",
                "9503:5>5:NC-2003-0103:Nota della 9503"
            }), "Ogni riga ha il suo storno e la sua nota.");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b AND JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Azione') = 'GeneraJsonFatture' AND JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Data') IS NOT NULL", ("@b", batchId)),
                Is.EqualTo(2), "La nota e' nel formato di DATA {Data, Testo, Azione}.");
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaMigrataUnaVolta), Is.True, "Inviata ma non scelta: non toccata.");
        });

        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.Scalar($"SELECT CONCAT(Stato, '|', IdUtente, '|', CountFatture) FROM {InvioSapTestData.Registro} WHERE BatchId = @b", ("@b", batchId)),
                Is.EqualTo("IN_ELABORAZIONE|integration-test-user|2"));
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(batchId));
            var log = InvioSapTestData.Scalar("SELECT CAST(JsonTransazione AS nvarchar(max)) FROM pfw.Log WHERE IdUtente = @u AND DescrizioneEvento = 'ISAP'", ("@u", InvioSapTestData.UtenteTest)) as string;
            Assert.That(log, Does.Contain("GENERAZIONE_JSON").And.Contain("reinvia").And.Contain("ReInvio"));
            Assert.That(log, Does.Not.Contain("Nota della"), "Il log non contiene le note.");
            var richiesta = FakeSynapseService.Instance.RichiesteGenerazione.Single();
            Assert.That(richiesta.BatchId, Is.EqualTo(batchId));
            Assert.That(richiesta.Parametri![0].MesiDaFatturare, Is.EqualTo(new[] { "200301" }));
        });

        var dopo = InvioSapTestData.FotografiaTabelle(prima.MaxMessaggio, InvioSapTestData.FattureRigoInviate);
        Assert.Multiple(() =>
        {
            Assert.That(dopo.Registro, Is.EqualTo(prima.Registro));
            Assert.That(dopo.Dettaglio, Is.EqualTo(prima.Dettaglio));
            Assert.That(dopo.Fatture, Is.EqualTo(prima.Fatture), "Nessuna fattura estranea al batch cambiata.");
            Assert.That(dopo.Messaggi, Is.EqualTo(prima.Messaggi));
            Assert.That(dopo.Log, Is.EqualTo(prima.Log));
        });
    }

    [Test]
    public async Task Reinvio_DiUnaSolaFattura_ShouldLasciareLAltraInviata()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia(Body(new F(9503, "solo la 9503", "NC-9503")));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.True, "Stesso rigo, non scelta: resta inviata.");
        });
    }

    [Test]
    public async Task Reinvio_NotaEStornoConCaratteriSpeciali_ShouldArrivareTaliEQualiAttraversoOpenJson()
    {
        InvioSapTestData.RilasciaLockComeData();
        const string nota = "Nota con \"apici\", 'singoli', \\ barre, a capo\n, tab\t, unicode üè e emoji 🚀";
        const string storno = "NC-'2003'-ü-🚀-\"x\"";
        var resp = await Reinvia(Body(new F(9502, nota, storno)));
        var batchId = (await Json(resp)).GetProperty("batchId").GetInt32();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(InvioSapTestData.Scalar($"SELECT NumeroDocStorno FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)), Is.EqualTo(storno));
            Assert.That(InvioSapTestData.Scalar($"SELECT JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Testo') FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)), Is.EqualTo(nota));
        });
    }

    [Test]
    public async Task Reinvio_NotaEStornoConSpazi_ShouldEssereScrittiSenzaSpaziEsterni()
    {
        InvioSapTestData.RilasciaLockComeData();
        var batchId = (await Json(await Reinvia(Body(new F(9502, "  nota  ", "  NC-1  "))))).GetProperty("batchId").GetInt32();
        Assert.That(RigheDettaglio(batchId), Is.EqualTo(new[] { "9502:5>5:NC-1:nota" }));
    }

    [Test]
    public async Task Reinvio_NotaDi2000Caratteri_ShouldEssereScrittaIntera()
    {
        InvioSapTestData.RilasciaLockComeData();
        var nota = new string('n', 2000);
        var batchId = (await Json(await Reinvia(Body(new F(9502, nota, "NC-1"))))).GetProperty("batchId").GetInt32();
        Assert.That(InvioSapTestData.Scalar($"SELECT LEN(JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Testo')) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b", ("@b", batchId)), Is.EqualTo(2000));
    }

    [Test]
    public async Task DopoLaChiusuraDiData_ShouldVedersiNelDettaglioLaNotaDiOgniFattura()
    {
        InvioSapTestData.RilasciaLockComeData();
        var batchId = (await Json(await Reinvia(Body(DueFatture)))).GetProperty("batchId").GetInt32();
        InvioSapTestData.ChiudiBatchComeData(batchId); // DATA: COMPLETATO, FatturaInviata = 1, DELETE del lock

        var dettaglio = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" }"""));
        var ultime = dettaglio.GetProperty("fatture").EnumerateArray()
            .Where(f => f.GetProperty("idFattura").GetInt64() is 9502 or 9503)
            .ToDictionary(f => f.GetProperty("idFattura").GetInt64(), f => f.GetProperty("storicoInvii").EnumerateArray().Last().GetProperty("testo").GetString());

        Assert.Multiple(() =>
        {
            Assert.That(ultime[9502], Is.EqualTo("Nota della 9502"));
            Assert.That(ultime[9503], Is.EqualTo("Nota della 9503"));
        });
    }

    // =============================================================================================
    // Lock: prima di ogni altra cosa
    // =============================================================================================

    [Test]
    public async Task LockAttivo_ShouldEssere409_AncheConBodyNonValido_SenzaScritture()
    {
        InvioSapTestData.AttivaLock(InvioSapTestData.BatchErrore);
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Reinvia("""{ "anno": 2003 }""");
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("LOCK_ATTIVO"));
            Assert.That(corpo.GetProperty("batchId").GetInt32(), Is.EqualTo(InvioSapTestData.BatchErrore));
        });
        NessunaScrittura(prima, "lock attivo");
    }

    [Test]
    public async Task RigaDiLockScadutaNonCancellata_ShouldEssere409_LockNonRilasciato_SenzaScritture()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Reinvia(Body(DueFatture));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(InvioSapTestData.BatchErrore), "Il BE non tocca la riga di DATA.");
        });
        Assert.That((await Json(resp)).GetProperty("motivo").GetString(), Is.EqualTo("LOCK_NON_RILASCIATO"));
        NessunaScrittura(prima, "riga di lock non rilasciata");
    }

    // =============================================================================================
    // 400 dal solo body
    // =============================================================================================

    [TestCase("{}", "Anno", TestName = "Reinvia400(body vuoto)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO" }""", "Selezionare", TestName = "Reinvia400(fatture assenti)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [] }""", "Selezionare", TestName = "Reinvia400(fatture vuote)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ null ] }""", "Fattura non valida", TestName = "Reinvia400(elemento null)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ { "nota": "n", "numeroDocumentoStorno": "s" } ] }""", "non valido", TestName = "Reinvia400(idFattura assente)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ { "idFattura": 9502, "numeroDocumentoStorno": "s" }, { "idFattura": 9503, "nota": " ", "numeroDocumentoStorno": "s" } ] }""", "nota e' obbligatoria per ogni fattura (fatture: 9502, 9503)", TestName = "Reinvia400(nota assente o vuota)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ { "idFattura": 9502, "nota": "n" } ] }""", "storno e' obbligatorio per ogni fattura (fatture: 9502)", TestName = "Reinvia400(storno assente)")]
    [TestCase("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ { "idFattura": 9502, "nota": "a", "numeroDocumentoStorno": "s" }, { "idFattura": 9502, "nota": "b", "numeroDocumentoStorno": "t" } ] }""", "ripetute", TestName = "Reinvia400(id ripetuto)")]
    public async Task BodyNonValido_ShouldEssere400_SenzaScritture(string body, string frammento)
    {
        InvioSapTestData.RilasciaLockComeData();
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Reinvia(body);
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain(frammento).IgnoreCase);
        });
        NessunaScrittura(prima, "body non valido");
    }

    [Test]
    public async Task MilleUnaFatture_ShouldEssere400_NonUnaQueryEnorme()
    {
        InvioSapTestData.RilasciaLockComeData();
        var fatture = Enumerable.Range(1, 1001).Select(i => new F(i, "n", "s")).ToArray();
        var resp = await Reinvia(Body(fatture));
        Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("al massimo 1000"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task StornoDi256Caratteri_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia(Body(new F(9502, "n", new string('s', 256))));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task IdNonNumerico_ShouldEssere500DelBinding_SenzaScritture_Caratterizzazione()
    {
        // Come per tutta l'API (v. Genera_BodyMalformato_…_Caratterizzazione): il binding fallisce prima dell'handler.
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia("""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "fatture": [ { "idFattura": "abc", "nota": "n", "numeroDocumentoStorno": "s" } ] }""");
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    // =============================================================================================
    // 404 e 400 dalle fatture lette in vista (stesse regole del Genera)
    // =============================================================================================

    [Test]
    public async Task FatturaInesistente_ShouldEssere404_SenzaScritture()
    {
        InvioSapTestData.RilasciaLockComeData();
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Reinvia(Body(new F(9502, "n", "s"), new F(long.MaxValue, "n", "s")));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima, "fattura inesistente");
    }

    [Test]
    public async Task FatturaMaiInviata_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia(Body(2003, 2, "PRIMO SALDO", new F(9505, "n", "s")));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("solo fatture gia' inviate"));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    [Test]
    public async Task FatturaDiUnAltroRigo_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia(Body(new F(9502, "n", "s"), new F(InvioSapTestData.FatturaVarSemestrale, "n", "s")));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("anno, al mese e alla tipologia"));
        });
    }

    [Test]
    public async Task FatturaInElaborazione_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        InvioSapTestData.Esegui("UPDATE pfd.FattureTestata SET FatturaInviata = NULL WHERE IdFattura = 9502");
        var resp = await Reinvia(Body(DueFatture));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    [Test]
    public async Task TipologiaOstile_ShouldEssere400FuoriGruppo_SenzaScritture()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Reinvia(Body(2003, 1, "PRIMO SALDO' OR 1=1 --", new F(9502, "n", "s")));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
        });
    }

    // =============================================================================================
    // Rigo cambiato fra lettura e scrittura, createRun, concorrenza
    // =============================================================================================

    [Test]
    public async Task FatturaCambiataFraLetturaEInsert_ShouldEssere409_GruppoCambiato_ERollback()
    {
        // Un trigger toglie la 9502 dalle inviate quando nasce il batch: l'INSERT ne trova una sola.
        InvioSapTestData.RilasciaLockComeData();
        InvioSapTestData.Esegui($@"CREATE OR ALTER TRIGGER pfw.trTestInvioSapReinvioCambiato ON {InvioSapTestData.Registro} AFTER INSERT AS
            UPDATE pfd.FattureTestata SET FatturaInviata = 0 WHERE IdFattura = 9502;");
        try
        {
            var resp = await Reinvia(Body(DueFatture));
            var corpo = await Json(resp);
            Assert.Multiple(() =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("GRUPPO_CAMBIATO"));
                Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Rollback.");
                Assert.That(InvioSapTestData.FatturaInviata(9502), Is.True, "Anche la modifica del trigger e' annullata.");
                Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
            });
        }
        finally
        {
            InvioSapTestData.Esegui("DROP TRIGGER IF EXISTS pfw.trTestInvioSapReinvioCambiato");
        }
    }

    [Test]
    public async Task CreateRun_ShouldPartireDopoIlCommit()
    {
        InvioSapTestData.RilasciaLockComeData();
        string? vistoDallaPipeline = null;
        FakeSynapseService.Instance.AllaRichiesta = r => vistoDallaPipeline = InvioSapTestData.Scalar($@"
            SET LOCK_TIMEOUT 2000;
            SELECT CONCAT(r.Stato, '|', (SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} d WHERE d.FkBatchId = r.BatchId AND d.NumeroDocStorno LIKE 'NC-2003-010_'),
                          '|', (SELECT COUNT(*) FROM pfd.FattureTestata WHERE IdFattura IN (9502, 9503) AND FatturaInviata IS NULL))
              FROM {InvioSapTestData.Registro} r WHERE r.BatchId = @b", ("@b", r.BatchId)) as string;

        var resp = await Reinvia(Body(DueFatture));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(vistoDallaPipeline, Is.EqualTo("IN_ELABORAZIONE|2|2"), "La pipeline legge righe gia' confermate.");
        });
    }

    [Test]
    public async Task SynapseKo_ShouldEssere500_ELeRigheRestano()
    {
        InvioSapTestData.RilasciaLockComeData();
        FakeSynapseService.Instance.Esito = false;

        var resp = await Reinvia(Body(DueFatture));
        var testo = await resp.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(testo, Does.Not.Contain("pfw.").And.Not.Contains("   at "));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1), "Punto D-m: il BE non annulla le righe confermate.");
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.Null);
        });
    }

    [Test]
    public async Task ReinviaEGeneraInParallelo_ShouldDareUn202EUn409_UnSoloBatch()
    {
        // Le due rotte condividono lo stesso lock: mai due batch insieme.
        InvioSapTestData.RilasciaLockComeData();
        FakeSynapseService.Instance.Ritardo = TimeSpan.FromSeconds(2);
        var batchPrima = InvioSapTestData.UltimoBatchStaccato();

        var risposte = await Task.WhenAll(
            Reinvia(Body(DueFatture)),
            Put(_factory, InvioSapTestData.RottaGenera, BodyGenera(Anno, InvioSapTestData.MesePrimoInvio, InvioSapTestData.TipologiaPrimoInvio, "primo invio")));
        var codici = risposte.Select(r => r.StatusCode).OrderBy(c => c).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codici, Is.EqualTo(new[] { HttpStatusCode.Accepted, HttpStatusCode.Conflict }));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
            Assert.That(InvioSapTestData.UltimoBatchStaccato(), Is.EqualTo(batchPrima + 1));
        });
    }

    [Test]
    public async Task DoppioClick_ShouldDareUn202EUn409()
    {
        InvioSapTestData.RilasciaLockComeData();
        var primo = await Reinvia(Body(DueFatture));
        var secondo = await Reinvia(Body(DueFatture));
        Assert.Multiple(() =>
        {
            Assert.That(primo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(secondo.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        });
    }
}
