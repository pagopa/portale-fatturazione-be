using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// End-to-end di <c>PUT api/fatture/invio-sap/genera</c> sul seed 2003 (be-invio-sap.md §3.3, §8.5, §13.3):
/// pipeline reale fino al DB, Synapse finto. Copre lock (vista, PK, riga non rilasciata), primo invio del rigo
/// intero, re-invio con storno, la prova "solo INSERT", il rollback su Synapse KO o eccezione e la concorrenza.
/// Ogni test parte dallo stato del seed (riga di lock scaduta) e lo ripristina.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapGeneraHttpTests
{
    private ApiTestFactory _factory = null!;
    private const int Anno = InvioSapTestData.Anno;

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

    private Task<HttpResponseMessage> Genera(string body) => Put(_factory, InvioSapTestData.RottaGenera, body);

    private static string PrimoInvio(string nota = "Primo invio di febbraio") =>
        BodyGenera(Anno, InvioSapTestData.MesePrimoInvio, InvioSapTestData.TipologiaPrimoInvio, nota);

    private static string ReInvio(long[]? ids = null, string? storno = "NC-2003-0100", string nota = "Re-invio dopo correzione") =>
        BodyGenera(Anno, InvioSapTestData.MeseReInvio, InvioSapTestData.TipologiaReInvio, nota, ids ?? InvioSapTestData.FattureRigoInviate, storno);

    private static void NessunaScrittura(InvioSapTestData.Fotografia prima, string perche)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, $"Nessun batch: {perche}");
            Assert.That(InvioSapTestData.ContaLogDeiTest(), Is.EqualTo(0), $"Nessun log: {perche}");
            Assert.That(InvioSapTestData.Scalar("SELECT COUNT(*) FROM pfd.Messaggi WHERE IdMessaggio > @m", ("@m", prima.MaxMessaggio)), Is.EqualTo(0), $"Nessun messaggio: {perche}");
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima), $"Righe preesistenti invariate: {perche}");
        });
    }

    // =============================================================================================
    // Lock
    // =============================================================================================

    [Test]
    public async Task LockAttivo_ShouldEssere409_PrimaDiOgniAltraCosa_SenzaScritture()
    {
        InvioSapTestData.AttivaLock(InvioSapTestData.BatchErrore);
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera("""{ "anno": 2003 }"""); // body non valido: il lock viene prima (richiesta di DATA)
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("LOCK_ATTIVO"));
            Assert.That(corpo.GetProperty("batchId").GetInt32(), Is.EqualTo(InvioSapTestData.BatchErrore));
            Assert.That(corpo.GetProperty("lockTimeout").ValueKind, Is.EqualTo(JsonValueKind.String));
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
        });
        NessunaScrittura(prima, "lock attivo");
    }

    [Test]
    public async Task RigaDiLockScadutaNonCancellata_ShouldEssere409_LockNonRilasciato_SenzaScritture()
    {
        // Stato del seed (C9): la vista e' vuota, ma la PK su FkIdAzione rifiuta l'INSERT. La pulizia e' di DATA.
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(PrimoInvio());
        var corpo = await Json(resp);

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("LOCK_NON_RILASCIATO"));
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(InvioSapTestData.BatchErrore), "Il BE non tocca la riga di DATA.");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
        });
        NessunaScrittura(prima, "riga di lock non rilasciata");
    }

    [Test]
    public async Task TipoAzioneMancante_ShouldEssere500_ERollback()
    {
        InvioSapTestData.RilasciaLockComeData();
        InvioSapTestData.Esegui($"DELETE FROM {InvioSapTestData.TipoAzione} WHERE IdAzione = @a", ("@a", InvioSapTestData.IdAzione));
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(PrimoInvio());

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        NessunaScrittura(prima, "configurazione mancante");
        // Pulisci() rimette la riga di TipoAzione.
    }

    // =============================================================================================
    // Primo invio: il rigo DA_INVIARE intero
    // =============================================================================================

    [Test]
    public async Task PrimoInvio_ShouldInserireEsattamenteLeFattureDelRigo_ConSoliInsert()
    {
        InvioSapTestData.RilasciaLockComeData();
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(PrimoInvio("Nota con \"apici\", \\ barre, a capo\n ed emoji 🚀"));
        var corpo = await Json(resp);
        var batchId = corpo.GetProperty("batchId").GetInt32();

        var righe = InvioSapTestData.Lista<string>($@"SELECT CONCAT(FkIdFattura, ':', FkStatoPrecedente, '>', FkStato, ':', ISNULL(NumeroDocStorno, '-'))
            FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("stato").GetString(), Is.EqualTo("IN_ELABORAZIONE"));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(InvioSapTestData.FattureRigoDaInviare));
            Assert.That(corpo.TryGetProperty("nomeFileJson", out _), Is.False, "Il nome lo scrive DATA.");
            Assert.That(righe, Is.EqualTo(new[] { "9505:4>5:-", "9506:4>5:-" }),
                "Solo 9505 e 9506: escluse 9507 (inviata), 9508 (NULL) e 9509 (altra tipologia).");
        });

        // Registro, lock, nota, log, messaggio.
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.Scalar($"SELECT CONCAT(Stato, '|', IdUtente, '|', CountFatture) FROM {InvioSapTestData.Registro} WHERE BatchId = @b", ("@b", batchId)),
                Is.EqualTo("IN_ELABORAZIONE|integration-test-user|2"), "CountFatture = NumeroFatture del rigo in griglia (D10).");
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(batchId));
            Assert.That(InvioSapTestData.Scalar($"SELECT DATEDIFF(MINUTE, lockStart, lockTimeout) FROM {InvioSapTestData.Lock}"), Is.EqualTo(60),
                "Timeout da pfw.TipoAzione (3.600.000 ms).");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b AND ISJSON(CAST(Nota AS nvarchar(max))) = 1 AND JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Azione') = 'GeneraJsonFatture'", ("@b", batchId)),
                Is.EqualTo(2));
            Assert.That(InvioSapTestData.Scalar($"SELECT JSON_VALUE(CAST(Nota AS nvarchar(max)), '$.Testo') FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b AND FkIdFattura = 9505", ("@b", batchId)),
                Is.EqualTo("Nota con \"apici\", \\ barre, a capo\n ed emoji 🚀"));
            Assert.That(InvioSapTestData.ContaLogDeiTest(), Is.EqualTo(1));
            Assert.That(InvioSapTestData.Scalar("SELECT CAST(JsonTransazione AS nvarchar(max)) FROM pfw.Log WHERE IdUtente = @u AND DescrizioneEvento = 'ISAP'", ("@u", InvioSapTestData.UtenteTest)) as string,
                Does.Contain("GENERAZIONE_JSON").And.Not.Contains("apici"), "Il log non contiene la nota.");
        });

        var messaggio = InvioSapTestData.Scalar("SELECT CONCAT(IdMessaggio, '|', Hash, '|', TipologiaDocumento, '|', Stato) FROM pfd.Messaggi WHERE IdMessaggio > @m", ("@m", prima.MaxMessaggio)) as string;
        var richiesta = FakeSynapseService.Instance.RichiesteGenerazione.Single();
        Assert.Multiple(() =>
        {
            Assert.That(messaggio, Is.EqualTo($"{richiesta.IdMessaggio}|{batchId}|INVIO SAP|1"));
            Assert.That(InvioSapTestData.Scalar("SELECT JSON_VALUE(CAST(Json AS nvarchar(max)), '$.batchId') FROM pfd.Messaggi WHERE IdMessaggio = @m", ("@m", richiesta.IdMessaggio)),
                Is.EqualTo(batchId.ToString()), "Il Json del messaggio non deve essere '{}' (proprieta' read-only dei tipi anonimi).");
            Assert.That(richiesta.BatchId, Is.EqualTo(batchId), "Il body di createRun porta il BatchId scritto.");
            Assert.That(richiesta.Parametri![0].MesiDaFatturare, Is.EqualTo(new[] { "200302" }));
        });

        // Solo INSERT: nessuna riga preesistente cambiata, FatturaInviata compresa (la scrive DATA).
        var dopo = InvioSapTestData.FotografiaTabelle(prima.MaxMessaggio);
        Assert.Multiple(() =>
        {
            Assert.That(dopo.Registro, Is.EqualTo(prima.Registro));
            Assert.That(dopo.Dettaglio, Is.EqualTo(prima.Dettaglio));
            Assert.That(dopo.Fatture, Is.EqualTo(prima.Fatture));
            Assert.That(dopo.Messaggi, Is.EqualTo(prima.Messaggi));
            Assert.That(dopo.Log, Is.EqualTo(prima.Log));
        });
    }

    [Test]
    public async Task PrimoInvio_RigoSenzaFattureDaInviare_ShouldEssere404_SenzaScritture()
    {
        InvioSapTestData.RilasciaLockComeData();
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(BodyGenera(Anno, 1, "PRIMO SALDO", "Rigo gia' inviato"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima, "rigo senza fatture da inviare");
    }

    [Test]
    public async Task PrimoInvio_ConStorno_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Genera(BodyGenera(Anno, 2, "PRIMO SALDO", "x", null, "NC-1"));
        Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("non e' ammesso"));
    }

    [Test]
    public async Task PrimoInvio_RigoCambiatoFraLetturaEInsert_ShouldEssere409_GruppoCambiato()
    {
        // Simula un cambio fra lettura e INSERT: un trigger aggiunge una fattura al rigo quando nasce il batch.
        InvioSapTestData.RilasciaLockComeData();
        InvioSapTestData.Esegui($@"CREATE OR ALTER TRIGGER pfw.trTestInvioSapGruppoCambiato ON {InvioSapTestData.Registro} AFTER INSERT AS
            UPDATE pfd.FattureTestata SET FatturaInviata = 0 WHERE IdFattura = 9507;");
        try
        {
            var resp = await Genera(PrimoInvio());
            var corpo = await Json(resp);
            Assert.Multiple(() =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("GRUPPO_CAMBIATO"));
                Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Rollback.");
                Assert.That(InvioSapTestData.Scalar("SELECT CAST(FatturaInviata AS int) FROM pfd.FattureTestata WHERE IdFattura = 9507"), Is.EqualTo(1),
                    "Anche la modifica del trigger e' annullata dal rollback.");
            });
        }
        finally
        {
            InvioSapTestData.Esegui("DROP TRIGGER IF EXISTS pfw.trTestInvioSapGruppoCambiato");
        }
    }

    // =============================================================================================
    // Re-invio: fatture scelte nel dettaglio di un rigo INVIATE
    // =============================================================================================

    [Test]
    public async Task ReInvio_ShouldInserireLeFattureScelte_DaInviataAInviata_ConStorno()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Genera(ReInvio());
        var corpo = await Json(resp);
        var batchId = corpo.GetProperty("batchId").GetInt32();
        var righe = InvioSapTestData.Lista<string>($@"SELECT CONCAT(FkIdFattura, ':', FkStatoPrecedente, '>', FkStato, ':', NumeroDocStorno)
            FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(righe, Is.EqualTo(new[] { "9502:5>5:NC-2003-0100", "9503:5>5:NC-2003-0100" }));
        });
    }

    [Test]
    public async Task ReInvio_FatturaMaiInviata_ShouldEssere404()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Genera(BodyGenera(Anno, 2, "PRIMO SALDO", "x", [9505], "NC-1"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "9505 non ha storico: per il re-invio non esiste (C1).");
    }

    [Test]
    public async Task ReInvio_FatturaDiUnAltroRigo_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Genera(ReInvio(ids: [9502, 9504]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("anno, al mese e alla tipologia"));
        });
    }

    [Test]
    public async Task ReInvio_FatturaInElaborazione_ShouldEssere400()
    {
        // 9502 diventa NULL (DATA ha avviato un altro batch): non si puo' re-inviare.
        InvioSapTestData.RilasciaLockComeData();
        InvioSapTestData.Esegui("UPDATE pfd.FattureTestata SET FatturaInviata = NULL WHERE IdFattura = 9502");
        var resp = await Genera(ReInvio());
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ReInvio_SenzaStorno_ShouldEssere400()
    {
        InvioSapTestData.RilasciaLockComeData();
        var resp = await Genera(ReInvio(storno: null));
        Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain("storno obbligatorio"));
    }

    // =============================================================================================
    // Rollback: Synapse KO o eccezione → nulla resta scritto
    // =============================================================================================

    [Test]
    public async Task SynapseKo_ShouldEssere500_ENessunaRiga()
    {
        InvioSapTestData.RilasciaLockComeData();
        FakeSynapseService.Instance.Esito = false;
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(PrimoInvio());

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(FakeSynapseService.Instance.RichiesteGenerazione, Has.Count.EqualTo(1), "La pipeline e' stata chiamata, ma ha rifiutato.");
            Assert.That(InvioSapTestData.ContaRighe(InvioSapTestData.Lock), Is.EqualTo(0), "Nemmeno il lock: l'admin puo' riprovare subito.");
        });
        NessunaScrittura(prima, "Synapse KO");
    }

    [Test]
    public async Task EccezioneDuranteLAvvio_ShouldFareRollbackCompleto()
    {
        InvioSapTestData.RilasciaLockComeData();
        FakeSynapseService.Instance.Eccezione = new HttpRequestException("rete giu'");
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(ReInvio());

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        NessunaScrittura(prima, "eccezione a meta' transazione");
        Assert.That(InvioSapTestData.ContaRighe(InvioSapTestData.Lock), Is.EqualTo(0));
    }

    // =============================================================================================
    // Concorrenza e ciclo completo con DATA
    // =============================================================================================

    [Test]
    public async Task DueGeneraInParallelo_ShouldDareUn202EUn409_UnSoloBatch()
    {
        InvioSapTestData.RilasciaLockComeData();
        FakeSynapseService.Instance.Ritardo = TimeSpan.FromSeconds(2);

        var risposte = await Task.WhenAll(Genera(PrimoInvio()), Genera(ReInvio()));
        var codici = risposte.Select(r => r.StatusCode).OrderBy(c => c).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(codici, Is.EqualTo(new[] { HttpStatusCode.Accepted, HttpStatusCode.Conflict }));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
            Assert.That(InvioSapTestData.ContaRighe(InvioSapTestData.Lock), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DoppioClick_ShouldDareUn202EUn409()
    {
        InvioSapTestData.RilasciaLockComeData();
        var primo = await Genera(PrimoInvio());
        var secondo = await Genera(PrimoInvio());
        var motivo = (await Json(secondo)).GetProperty("motivo").GetString();
        Assert.Multiple(() =>
        {
            Assert.That(primo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(secondo.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(motivo, Is.EqualTo("LOCK_ATTIVO"));
        });
    }

    [Test]
    public async Task DopoLaChiusuraDiData_ShouldPoterGenerareDiNuovo_ELeFattureCompaionoNelDettaglio()
    {
        InvioSapTestData.RilasciaLockComeData();
        var corpo = await Json(await Genera(PrimoInvio()));
        var batchId = corpo.GetProperty("batchId").GetInt32();

        InvioSapTestData.ChiudiBatchComeData(batchId); // DATA: COMPLETATO, FatturaInviata = 1, DELETE del lock

        var lockDopo = await Get(_factory, InvioSapTestData.RottaLock);
        var dettaglio = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 2, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" }"""));
        var reInvio = await Genera(BodyGenera(Anno, 2, "PRIMO SALDO", "Re-invio dopo il primo", [9505], "NC-2003-0200"));

        Assert.Multiple(() =>
        {
            Assert.That(lockDopo.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(dettaglio.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("idFattura").GetInt64()),
                Is.EquivalentTo(new long[] { 9505, 9506, 9507 }), "Le fatture del primo invio ora hanno storico.");
            Assert.That(reInvio.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), "BE-SAP-M11: dopo la chiusura si genera subito.");
        });
    }
}
