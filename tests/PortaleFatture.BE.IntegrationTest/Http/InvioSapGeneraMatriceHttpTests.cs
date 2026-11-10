using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Matrice dei casi di <c>PUT api/fatture/invio-sap/genera</c> (be-invio-sap.md §3.3): ogni riga combina
/// <c>allSelected</c>, <c>idFatture</c>, <c>idFattureEscluse</c>, storno e stato del rigo, e dichiara l'esito
/// atteso e l'insieme esatto delle fatture che devono finire nel batch. Il Genera e' l'operazione critica
/// dell'area: su ogni riga valgono in piu' gli stessi invarianti.
///   - esito diverso da 202: nessuna scrittura e pipeline non chiamata;
///   - esito 202: nel batch ci sono solo le fatture attese, solo quelle passano a "in elaborazione", tutte le altre
///     fatture del seed restano come prima, il lock porta il batch e la pipeline viene chiamata una volta con quel batch.
/// Righi del seed 2003:
///   R1 = PRIMO SALDO gennaio, solo inviate (9502, 9503); nello stesso mese 9501 e' un ANTICIPO inviato;
///   R2 = PRIMO SALDO febbraio, tre stati: 9505 e 9506 da inviare, 9507 inviata, 9508 in elaborazione;
///   R3 = ACCONTO marzo, sole fatture da inviare.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapGeneraMatriceHttpTests
{
    private ApiTestFactory _factory = null!;
    private const string NC = "NC-2003-0300";

    public sealed record Caso(
        string Nome, int Mese, string Tipologia, bool? AllSelected, long[]? Ids, long[]? Escluse, string? Storno,
        int Atteso, long[] FattureAttese, string? Nota = "Matrice del Genera", int? Anno = InvioSapTestData.Anno)
    {
        public override string ToString() => Nome;
    }

    private static Caso R1(string nome, bool? all, long[]? ids, long[]? escluse, string? storno, int atteso, params long[] attese)
        => new(nome, 1, "PRIMO SALDO", all, ids, escluse, storno, atteso, attese);

    private static Caso R2(string nome, bool? all, long[]? ids, long[]? escluse, string? storno, int atteso, params long[] attese)
        => new(nome, 2, "PRIMO SALDO", all, ids, escluse, storno, atteso, attese);

    public static IEnumerable<TestCaseData> Matrice()
    {
        Caso[] casi =
        [
            // ---- allSelected = true: il rigo meno le escluse; idFatture non viene letto --------------------------
            R1("T01 tutto, senza escluse", true, null, null, NC, 202, 9502, 9503),
            R1("T02 tutto, escluse vuote", true, null, [], NC, 202, 9502, 9503),
            R1("T03 tutto, una esclusa", true, null, [9502], NC, 202, 9503),
            R1("T04 tutto, idFatture in conflitto con le escluse: vincono le escluse", true, [9503], [9503], NC, 202, 9502),
            R1("T05 tutto, idFatture di un altro rigo: ignorato", true, [9504], null, NC, 202, 9502, 9503),
            R1("T06 tutto, idFatture vuoto: ignorato", true, [], null, NC, 202, 9502, 9503),
            R1("T07 tutto, idFatture non valido: ignorato", true, [0, 0, -1], null, NC, 202, 9502, 9503),
            R1("T08 tutto, tutte escluse", true, null, [9502, 9503], NC, 400),
            R1("T09 tutto, senza storno", true, null, null, null, 400),
            R1("T10 tutto, storno vuoto", true, null, null, "", 400),
            R1("T11 tutto, storno di soli spazi", true, null, null, "   ", 400),
            R1("T12 tutto, storno oltre 255 caratteri", true, null, null, new string('9', 256), 400),
            R1("T13 tutto, esclusa di un altro rigo (altro mese)", true, null, [9504], NC, 400),
            R1("T14 tutto, esclusa dello stesso mese ma di altra tipologia", true, null, [9501], NC, 400),
            R1("T15 tutto, esclusa di un altro mese della stessa tipologia", true, null, [9507], NC, 400),
            R1("T16 tutto, esclusa mai inviata (senza dettaglio)", true, null, [9505], NC, 404),
            R1("T17 tutto, esclusa inesistente", true, null, [987654321], NC, 404),
            R1("T18 tutto, una esclusa buona e una inesistente", true, null, [9502, 987654321], NC, 404),
            R1("T19 tutto, esclusa zero", true, null, [0], NC, 400),
            R1("T20 tutto, esclusa negativa", true, null, [-9502], NC, 400),
            R1("T21 tutto, esclusa ripetuta", true, null, [9502, 9502], NC, 400),
            R2("T22 tutto sul rigo a tre stati: solo l'inviata", true, null, null, NC, 202, 9507),
            R2("T23 tutto sul rigo a tre stati, esclusa l'unica inviata", true, null, [9507], NC, 400),
            R2("T24 tutto sul rigo a tre stati, senza storno: non e' un primo invio", true, null, null, null, 400),
            R2("T25 tutto sul rigo a tre stati, esclusa una da inviare (senza dettaglio)", true, null, [9505], NC, 404),
            R2("T26 tutto sul rigo a tre stati, esclusa quella in elaborazione (senza dettaglio)", true, null, [9508], NC, 404),
            new("T27 tutto su un rigo con sole fatture da inviare", 3, "ACCONTO", true, null, null, NC, 404, []),
            new("T28 tutto su un rigo che non esiste", 12, "PRIMO SALDO", true, null, null, NC, 404, []),
            new("T29 tutto su una tipologia che non esiste", 1, "TIPOLOGIA INESISTENTE", true, null, null, NC, 404, []),
            new("T30 tutto, tipologia con minuscole e spazi", 1, "  primo saldo ", true, null, null, NC, 202, [9502, 9503]),

            // ---- allSelected = false: vale idFatture; le escluse non vengono lette ------------------------------
            R1("F01 elenco completo", false, [9502, 9503], null, NC, 202, 9502, 9503),
            R1("F02 elenco di una fattura", false, [9503], null, NC, 202, 9503),
            R1("F03 elenco, la stessa fattura anche fra le escluse: ignorate", false, [9502], [9502], NC, 202, 9502),
            R1("F04 elenco, escluse non valide: ignorate", false, [9502], [0, 0, -1], NC, 202, 9502),
            R1("F05 elenco, escluse inesistenti: ignorate", false, [9502], [987654321], NC, 202, 9502),
            R1("F06 elenco vuoto", false, [], null, NC, 400),
            R1("F07 elenco vuoto con escluse", false, [], [9502], NC, 400),
            R1("F08 elenco senza storno", false, [9502], null, null, 400),
            R1("F09 elenco con fattura di un altro rigo", false, [9502, 9504], null, NC, 400),
            R1("F10 elenco con fattura mai inviata", false, [9502, 9505], null, NC, 404),
            R1("F11 elenco con id ripetuto", false, [9502, 9502], null, NC, 400),
            R1("F12 elenco con id zero", false, [0], null, NC, 400),
            R2("F13 elenco sul rigo a tre stati: l'inviata", false, [9507], null, NC, 202, 9507),
            R2("F14 elenco sul rigo a tre stati: una da inviare", false, [9505], null, NC, 404),
            R2("F15 elenco sul rigo a tre stati: quella in elaborazione", false, [9508], null, NC, 404),

            // ---- allSelected = false senza elenco: primo invio del rigo intero, come prima ----------------------
            R2("P01 primo invio", false, null, null, null, 202, 9505, 9506),
            R2("P02 primo invio, escluse presenti: ignorate", false, null, [9505], null, 202, 9505, 9506),
            R2("P03 primo invio con storno", false, null, null, NC, 400),
            R1("P04 primo invio su un rigo senza fatture da inviare", false, null, null, null, 404),
            new("P05 primo invio su un rigo che non esiste", 12, "PRIMO SALDO", false, null, null, null, 404, []),

            // ---- allSelected assente: come false (contratto precedente) -----------------------------------------
            R1("N01 assente, elenco", null, [9502], null, NC, 202, 9502),
            R1("N02 assente, elenco ed escluse: escluse ignorate", null, [9502, 9503], [9503], NC, 202, 9502, 9503),
            R2("N03 assente, senza elenco: primo invio", null, null, null, null, 202, 9505, 9506),
            R2("N04 assente, senza elenco, con escluse: primo invio, escluse ignorate", null, null, [9506], null, 202, 9505, 9506),
            R1("N05 assente, solo escluse e storno: primo invio con storno", null, null, [9502], NC, 400),
            R1("N06 assente, elenco vuoto", null, [], null, NC, 400),

            // ---- campi comuni: valgono in ogni modalita' ---------------------------------------------------------
            new("C01 tutto, nota assente", 1, "PRIMO SALDO", true, null, null, NC, 400, [], Nota: null),
            new("C02 tutto, nota di soli spazi", 1, "PRIMO SALDO", true, null, null, NC, 400, [], Nota: "  "),
            new("C03 tutto, nota oltre 2000 caratteri", 1, "PRIMO SALDO", true, null, null, NC, 400, [], Nota: new string('n', 2001)),
            new("C04 tutto, nota di 2000 caratteri", 1, "PRIMO SALDO", true, null, null, NC, 202, [9502, 9503], Nota: new string('n', 2000)),
            new("C05 tutto, mese 13", 13, "PRIMO SALDO", true, null, null, NC, 400, []),
            new("C06 tutto, mese zero", 0, "PRIMO SALDO", true, null, null, NC, 400, []),
            new("C07 tutto, tipologia vuota", 1, "", true, null, null, NC, 400, []),
            new("C08 tutto, anno assente", 1, "PRIMO SALDO", true, null, null, NC, 400, [], Anno: null),
            new("C09 tutto, anno sotto il minimo", 1, "PRIMO SALDO", true, null, null, NC, 400, [], Anno: 1999),
            new("C10 tutto, anno senza fatture", 1, "PRIMO SALDO", true, null, null, NC, 404, [], Anno: 2001),
            new("C11 tutto, nota con apici e caratteri speciali", 1, "PRIMO SALDO", true, null, [9503], NC, 202, [9502],
                Nota: "O'Brien \"x\" <b>;--</b> € àèì"),
        ];
        return casi.Select(c => new TestCaseData(c).SetName($"Matrice_Genera({c.Nome})"));
    }

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
        InvioSapTestData.RilasciaLockComeData();
    }

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    /// <summary>Stato di invio di tutte le fatture del seed 2003, per verificare che fuori dal batch non cambi nulla.</summary>
    private static Dictionary<long, string> StatoFattureSeed() =>
        InvioSapTestData.Lista<string>(@"SELECT CONCAT(IdFattura, '=', ISNULL(CAST(CAST(FatturaInviata AS int) AS varchar(1)), 'N'))
              FROM pfd.FattureTestata WHERE AnnoRiferimento = 2003 ORDER BY IdFattura")
            .Select(r => r.Split('='))
            .ToDictionary(p => long.Parse(p[0]), p => p[1]);

    [TestCaseSource(nameof(Matrice))]
    public async Task Matrice_Genera(Caso caso)
    {
        var body = JsonSerializer.Serialize(new
        {
            anno = caso.Anno, mese = caso.Mese, tipologiaFattura = caso.Tipologia, nota = caso.Nota,
            allSelected = caso.AllSelected, idFatture = caso.Ids, idFattureEscluse = caso.Escluse, numeroDocumentoStorno = caso.Storno
        });
        var fotoPrima = InvioSapTestData.FotografiaTabelle();
        var statoPrima = StatoFattureSeed();

        var resp = await Put(_factory, InvioSapTestData.RottaGenera, body);
        var testo = await resp.Content.ReadAsStringAsync();

        Assert.That((int)resp.StatusCode, Is.EqualTo(caso.Atteso), testo);
        Assert.That(testo, Does.Not.Contain("   at ").And.Not.Contain("SqlException"), "Nessun dettaglio tecnico nella risposta.");

        if (caso.Atteso != 202)
        {
            Assert.Multiple(() =>
            {
                Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Nessun batch");
                Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(fotoPrima), "Nessuna scrittura");
                Assert.That(StatoFattureSeed(), Is.EqualTo(statoPrima), "Nessuna fattura cambiata");
                Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, "Pipeline non chiamata");
            });
            return;
        }

        var corpo = JsonDocument.Parse(testo).RootElement;
        var batchId = corpo.GetProperty("batchId").GetInt32();
        var nelBatch = InvioSapTestData.Lista<long>(
            $"SELECT FkIdFattura FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));
        var statoDopo = StatoFattureSeed();
        var atteseDopo = statoPrima.ToDictionary(p => p.Key, p => caso.FattureAttese.Contains(p.Key) ? "N" : p.Value);

        Assert.Multiple(() =>
        {
            Assert.That(nelBatch, Is.EqualTo(caso.FattureAttese), "Nel batch le sole fatture attese");
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(caso.FattureAttese.Length));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(caso.FattureAttese));
            Assert.That(statoDopo, Is.EqualTo(atteseDopo), "In elaborazione le sole fatture del batch; le altre invariate");
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.EqualTo(new[] { batchId }), "Un solo batch");
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(batchId), "Il lock porta il batch");
            Assert.That(FakeSynapseService.Instance.RichiesteGenerazione.Select(r => r.BatchId), Is.EqualTo(new[] { batchId }),
                "Pipeline chiamata una volta, con il batch creato");
        });
    }

    // =============================================================================================
    // Fasi di errore con "seleziona tutto": stesse garanzie del primo invio e del re-invio a elenco
    // =============================================================================================

    private Task<HttpResponseMessage> GeneraTutto(long[]? escluse = null) => Put(_factory, InvioSapTestData.RottaGenera,
        JsonSerializer.Serialize(new
        {
            anno = InvioSapTestData.Anno, mese = 1, tipologiaFattura = "PRIMO SALDO", nota = "Seleziona tutto",
            allSelected = true, idFattureEscluse = escluse, numeroDocumentoStorno = NC
        }));

    [Test]
    public async Task SelezionaTutto_RigoCambiatoFraLetturaEInsert_ShouldEssere409_GruppoCambiato_ERollback()
    {
        // Fra il conteggio e l'INSERT una fattura del rigo esce dallo stato "inviata": un trigger la cambia quando nasce il batch.
        InvioSapTestData.Esegui($@"CREATE OR ALTER TRIGGER pfw.trTestInvioSapGruppoCambiato ON {InvioSapTestData.Registro} AFTER INSERT AS
            UPDATE pfd.FattureTestata SET FatturaInviata = NULL WHERE IdFattura = 9503;");
        try
        {
            var statoPrima = StatoFattureSeed();
            var resp = await GeneraTutto();
            var corpo = await Json(resp);
            Assert.Multiple(() =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("GRUPPO_CAMBIATO"));
                Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Rollback: nessun batch.");
                Assert.That(StatoFattureSeed(), Is.EqualTo(statoPrima), "Rollback: nessuna fattura cambiata, nemmeno dal trigger.");
                Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
            });
        }
        finally
        {
            InvioSapTestData.Esegui("DROP TRIGGER IF EXISTS pfw.trTestInvioSapGruppoCambiato");
        }
    }

    [Test]
    public async Task SelezionaTutto_FatturaAggiuntaAlRigoFraLetturaEInsert_ShouldEssere409_GruppoCambiato()
    {
        // Il caso opposto: dopo il conteggio una fattura in piu' diventa inviata e il gruppo cresce.
        InvioSapTestData.Esegui("UPDATE pfd.FattureTestata SET FatturaInviata = NULL WHERE IdFattura = 9503");
        InvioSapTestData.Esegui($@"CREATE OR ALTER TRIGGER pfw.trTestInvioSapGruppoCambiato ON {InvioSapTestData.Registro} AFTER INSERT AS
            UPDATE pfd.FattureTestata SET FatturaInviata = 1 WHERE IdFattura = 9503;");
        try
        {
            var resp = await GeneraTutto();
            var corpo = await Json(resp);
            Assert.Multiple(() =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), "Contata una fattura, ne arriverebbero due: non si prosegue.");
                Assert.That(corpo.GetProperty("motivo").GetString(), Is.EqualTo("GRUPPO_CAMBIATO"));
                Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
                Assert.That(InvioSapTestData.FatturaInviata(9502), Is.True, "Rollback.");
            });
        }
        finally
        {
            InvioSapTestData.Esegui("DROP TRIGGER IF EXISTS pfw.trTestInvioSapGruppoCambiato");
        }
    }

    [TestCase(false, TestName = "SelezionaTutto_PipelineNonAvviata_ShouldEssere500_ELeRigheRestano(Synapse risponde KO)")]
    [TestCase(true, TestName = "SelezionaTutto_PipelineNonAvviata_ShouldEssere500_ELeRigheRestano(eccezione durante l'avvio)")]
    public async Task SelezionaTutto_PipelineNonAvviata_ShouldEssere500_ELeRigheRestano(bool eccezione)
    {
        if (eccezione) FakeSynapseService.Instance.Eccezione = new InvalidOperationException("rete non raggiungibile");
        else FakeSynapseService.Instance.Esito = false;

        var resp = await GeneraTutto(escluse: [9503]);
        var testo = await resp.Content.ReadAsStringAsync();
        var batch = InvioSapTestData.BatchDeiTest();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(testo, Does.Not.Contain("pfw.").And.Not.Contain("   at ").And.Not.Contain("rete non raggiungibile"));
            Assert.That(batch, Has.Count.EqualTo(1), "Il commit e' avvenuto prima della pipeline: il batch resta (D-m).");
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.Null, "Resta in elaborazione.");
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.True, "Esclusa: mai toccata.");
        });

        FakeSynapseService.Instance.Reset();
        var secondo = await GeneraTutto();
        Assert.That(secondo.StatusCode, Is.EqualTo(HttpStatusCode.Conflict), "Il lock resta attivo finche' DATA non lo rimuove.");
    }

    [Test]
    public async Task SelezionaTutto_DopoUnBatchChiusoDaData_ShouldPoterRigenerare()
    {
        var primo = await Json(await GeneraTutto());
        InvioSapTestData.ChiudiBatchComeData(primo.GetProperty("batchId").GetInt32());

        var resp = await GeneraTutto(escluse: [9502]);
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(new long[] { 9503 }));
            Assert.That(corpo.GetProperty("batchId").GetInt32(), Is.GreaterThan(primo.GetProperty("batchId").GetInt32()));
        });
    }

    [Test]
    public async Task SelezionaTutto_SubitoDopoUnAltroSelezionaTutto_ShouldEssere409_SenzaSecondoBatch()
    {
        var primo = await GeneraTutto();
        var secondo = await GeneraTutto();
        Assert.Multiple(() =>
        {
            Assert.That(primo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(secondo.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
        });
    }
}
