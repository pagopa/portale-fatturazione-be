using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// "Seleziona tutto" del dettaglio paginato su <c>PUT api/fatture/invio-sap/genera</c> (be-invio-sap.md §3.3):
/// con <c>allSelected = true</c> il Genera re-invia tutte le fatture inviate del rigo tranne
/// <c>idFattureEscluse</c> e ignora <c>idFatture</c>; con <c>allSelected</c> false o assente vale <c>idFatture</c>
/// e le escluse vengono ignorate. Seed 2003: il rigo INVIATE di PRIMO SALDO gennaio ha 9502 e 9503; PRIMO SALDO
/// febbraio ha tre stati (9505 e 9506 da inviare, 9507 inviata, 9508 in elaborazione).
/// Ogni test parte dallo stato del seed e lo ripristina.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapSelezionaTuttoHttpTests
{
    private ApiTestFactory _factory = null!;
    private const int Anno = InvioSapTestData.Anno;
    private const string Storno = "NC-2003-0200";

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

    private Task<HttpResponseMessage> Genera(string body) => Put(_factory, InvioSapTestData.RottaGenera, body);

    private static string Body(int mese = InvioSapTestData.MeseReInvio, string tipologia = InvioSapTestData.TipologiaReInvio,
        bool? allSelected = true, long[]? escluse = null, long[]? ids = null, string? storno = Storno)
        => JsonSerializer.Serialize(new
        {
            anno = Anno, mese, tipologiaFattura = tipologia, nota = "Re-invio con seleziona tutto",
            allSelected, idFatture = ids, idFattureEscluse = escluse, numeroDocumentoStorno = storno
        });

    private static List<long> FattureDelBatch(int batchId) =>
        InvioSapTestData.Lista<long>($"SELECT FkIdFattura FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));

    private static async Task SenzaStackTrace(HttpResponseMessage resp)
    {
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.That(testo, Does.Not.Contain("   at ").And.Not.Contain("SqlException").And.Not.Contain("Exception:"));
    }

    private static void NessunaScrittura(InvioSapTestData.Fotografia prima)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Nessun batch");
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima), "Righe preesistenti invariate");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, "Pipeline non chiamata");
        });
    }

    // =============================================================================================
    // Selezione
    // =============================================================================================

    [Test]
    public async Task SelezionaTutto_ShouldReInviareTutteLeFattureInviate()
    {
        var resp = await Genera(Body());
        var corpo = await Json(resp);
        var batchId = corpo.GetProperty("batchId").GetInt32();
        var righe = InvioSapTestData.Lista<string>($@"SELECT CONCAT(FkIdFattura, ':', FkStatoPrecedente, '>', FkStato, ':', NumeroDocStorno)
            FROM {InvioSapTestData.Dettaglio} WHERE FkBatchId = @b ORDER BY FkIdFattura", ("@b", batchId));
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(2));
            Assert.That(corpo.GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()), Is.EqualTo(InvioSapTestData.FattureRigoInviate));
            Assert.That(righe, Is.EqualTo(new[] { $"9502:5>5:{Storno}", $"9503:5>5:{Storno}" }));
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.Null, "Del rigo: in elaborazione.");
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.Null, "Del rigo: in elaborazione.");
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaMigrataUnaVolta), Is.True, "Stesso mese, altra tipologia: non toccata.");
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaVarSemestrale), Is.True, "Altro rigo: non toccata.");
            Assert.That(FakeSynapseService.Instance.Chiamate, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task SelezionaTutto_ConEscluse_ShouldReInviareIlRigoMenoLeEscluse()
    {
        var resp = await Genera(Body(escluse: [9503]));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(new long[] { 9502 }));
            Assert.That(corpo.GetProperty("numeroFatture").GetInt32(), Is.EqualTo(1));
            Assert.That(InvioSapTestData.FatturaInviata(9502), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.True, "Esclusa: resta inviata.");
        });
    }

    [Test]
    public async Task SelezionaTutto_ShouldIgnorareIdFatture()
    {
        // idFatture punta a una sola fattura (e a una di un altro rigo): con allSelected = true non viene letto.
        var resp = await Genera(Body(ids: [9502, InvioSapTestData.FatturaVarSemestrale]));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(InvioSapTestData.FattureRigoInviate));
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaVarSemestrale), Is.True);
        });
    }

    [TestCase(false, TestName = "SenzaSelezionaTutto_ShouldValereIdFatture_EIgnorareLeEscluse(allSelected: false)")]
    [TestCase(null, TestName = "SenzaSelezionaTutto_ShouldValereIdFatture_EIgnorareLeEscluse(allSelected: assente)")]
    public async Task SenzaSelezionaTutto_ShouldValereIdFatture_EIgnorareLeEscluse(bool? allSelected)
    {
        // La stessa fattura e' scelta ed "esclusa": le escluse non contano, quindi viene re-inviata.
        var resp = await Genera(Body(allSelected: allSelected, ids: [9502], escluse: [9502, 0, 0]));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(new long[] { 9502 }));
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.True, "Non scelta: resta inviata.");
        });
    }

    [Test]
    public async Task SelezionaTutto_RigoConTreStati_ShouldPrendereSoloLeInviate()
    {
        // PRIMO SALDO febbraio: 9505 e 9506 da inviare, 9507 inviata, 9508 in elaborazione. Solo 9507 e' "inviata".
        var resp = await Genera(Body(mese: InvioSapTestData.MesePrimoInvio, tipologia: InvioSapTestData.TipologiaPrimoInvio));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(new[] { InvioSapTestData.FatturaStessoMeseInviata }));
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaStessoMeseInviata), Is.Null);
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.False, "Da inviare: il seleziona tutto non e' un primo invio.");
            Assert.That(InvioSapTestData.FatturaInviata(9506), Is.False, "Da inviare: il seleziona tutto non e' un primo invio.");
        });
    }

    [TestCase(false, TestName = "SenzaSelezionaTutto_SenzaIdFatture_ShouldRestareIlPrimoInvio(allSelected: false)")]
    [TestCase(null, TestName = "SenzaSelezionaTutto_SenzaIdFatture_ShouldRestareIlPrimoInvio(allSelected: assente)")]
    public async Task SenzaSelezionaTutto_SenzaIdFatture_ShouldRestareIlPrimoInvio(bool? allSelected)
    {
        var resp = await Genera(Body(mese: InvioSapTestData.MesePrimoInvio, tipologia: InvioSapTestData.TipologiaPrimoInvio,
            allSelected: allSelected, storno: null));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(InvioSapTestData.FattureRigoDaInviare));
            Assert.That(InvioSapTestData.FatturaInviata(InvioSapTestData.FatturaStessoMeseInviata), Is.True, "Inviata: il primo invio non la tocca.");
        });
    }

    // =============================================================================================
    // Rifiuti: nessuna scrittura
    // =============================================================================================

    [Test]
    public async Task SelezionaTutto_SenzaStorno_ShouldEssere400_ENonDiventareUnPrimoInvio()
    {
        // Sul rigo con fatture da inviare: senza storno la richiesta va respinta, non letta come primo invio.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(mese: InvioSapTestData.MesePrimoInvio, tipologia: InvioSapTestData.TipologiaPrimoInvio, storno: null));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("storno obbligatorio"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_TutteEscluse_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: [9502, 9503]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("Selezionare almeno una fattura"));
        });
        NessunaScrittura(prima);
    }

    [TestCase(3, "ACCONTO", TestName = "SelezionaTutto_RigoSenzaInviate_ShouldEssere404(rigo con sole fatture da inviare)")]
    [TestCase(11, "PRIMO SALDO", TestName = "SelezionaTutto_RigoSenzaInviate_ShouldEssere404(rigo inesistente)")]
    public async Task SelezionaTutto_RigoSenzaInviate_ShouldEssere404(int mese, string tipologia)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(mese: mese, tipologia: tipologia));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_EsclusaInesistente_ShouldEssere404()
    {
        // 9505 non ha storico (C1): per il dettaglio non esiste, come nel re-invio a elenco.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: [9505]));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_EsclusaDiUnAltroRigo_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: [9503, InvioSapTestData.FatturaVarSemestrale]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("anno, al mese e alla tipologia"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_FatturaPassataInElaborazione_ShouldRestareFuori()
    {
        // 9503 diventa NULL (un altro batch la sta elaborando): il seleziona tutto prende solo cio' che e' inviato ora.
        InvioSapTestData.Esegui("UPDATE pfd.FattureTestata SET FatturaInviata = NULL WHERE IdFattura = 9503");
        var resp = await Genera(Body());
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(new long[] { 9502 }));
        });
    }

    [Test]
    public async Task SelezionaTutto_ConLockAttivo_ShouldEssere409_SenzaScritture()
    {
        InvioSapTestData.AttivaLock(InvioSapTestData.BatchErrore);
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body());
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        NessunaScrittura(prima);
    }

    // =============================================================================================
    // Adversarial
    // =============================================================================================

    private static string BodyGrezzo(string campi) =>
        $$"""{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "nota": "x", "numeroDocumentoStorno": "NC-1", {{campi}} }""";

    [TestCase(""" "allSelected": true, "idFattureEscluse": [0] """, TestName = "SelezionaTutto_EscluseNonValide_ShouldEssere400(esclusa zero)")]
    [TestCase(""" "allSelected": true, "idFattureEscluse": [-9502] """, TestName = "SelezionaTutto_EscluseNonValide_ShouldEssere400(esclusa negativa)")]
    [TestCase(""" "allSelected": true, "idFattureEscluse": [9503, 9503] """, TestName = "SelezionaTutto_EscluseNonValide_ShouldEssere400(esclusa ripetuta)")]
    [TestCase(""" "allSelected": true, "idFattureEscluse": [-9223372036854775808] """, TestName = "SelezionaTutto_EscluseNonValide_ShouldEssere400(long.MinValue)")]
    public async Task SelezionaTutto_EscluseNonValide_ShouldEssere400(string campi)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(BodyGrezzo(campi));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await SenzaStackTrace(resp);
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_EscluseOltreIlTetto_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: Enumerable.Range(1, 1001).Select(i => 100_000L + i).ToArray()));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("escludere al massimo 1000"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_MilleEscluseInesistenti_ShouldEssere404_SenzaErroriDiParametri()
    {
        // Al tetto: mille id nella IN della lettura. Non esistono, quindi 404 e non un errore del driver.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: Enumerable.Range(1, 1000).Select(i => 100_000L + i).ToArray()));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_EsclusaEnorme_ShouldEssere404()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(escluse: [long.MaxValue]));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [TestCase(""" "allSelected": true, "idFatture": [0, 0, -1] """, TestName = "SelezionaTutto_IdFattureNonValide_ShouldEssereIgnorate(id non validi e ripetuti)")]
    [TestCase(""" "allSelected": true, "idFatture": [] """, TestName = "SelezionaTutto_IdFattureNonValide_ShouldEssereIgnorate(lista vuota)")]
    [TestCase(""" "allSelected": true, "idFatture": null, "idFattureEscluse": null """, TestName = "SelezionaTutto_IdFattureNonValide_ShouldEssereIgnorate(liste null)")]
    [TestCase(""" "AllSelected": true, "IdFattureEscluse": [] """, TestName = "SelezionaTutto_IdFattureNonValide_ShouldEssereIgnorate(nomi con la maiuscola)")]
    public async Task SelezionaTutto_IdFattureNonValide_ShouldEssereIgnorate(string campi)
    {
        var resp = await Genera(BodyGrezzo(campi));
        var corpo = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(FattureDelBatch(corpo.GetProperty("batchId").GetInt32()), Is.EqualTo(InvioSapTestData.FattureRigoInviate));
        });
    }

    /// <summary>Stessa caratterizzazione delle altre rotte: l'errore di binding del body e' un 500 su tutta l'API.</summary>
    [TestCase(""" "allSelected": "true" """, TestName = "SelezionaTutto_BodyMalformato_ShouldRispondere500_Caratterizzazione(allSelected come stringa)")]
    [TestCase(""" "allSelected": 1 """, TestName = "SelezionaTutto_BodyMalformato_ShouldRispondere500_Caratterizzazione(allSelected come numero)")]
    [TestCase(""" "allSelected": true, "idFattureEscluse": "9503 OR 1=1" """, TestName = "SelezionaTutto_BodyMalformato_ShouldRispondere500_Caratterizzazione(injection nelle escluse)")]
    [TestCase(""" "allSelected": true, "idFattureEscluse": ["9503'; DROP TABLE pfw.DettaglioInvioSAP --"] """, TestName = "SelezionaTutto_BodyMalformato_ShouldRispondere500_Caratterizzazione(injection in un id escluso)")]
    public async Task SelezionaTutto_BodyMalformato_ShouldRispondere500_Caratterizzazione(string campi)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(BodyGrezzo(campi));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        await SenzaStackTrace(resp);
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_TipologiaConInjection_ShouldEssere404_SenzaScritture()
    {
        // La tipologia e' un parametro: un testo ostile non trova fatture e non altera la query.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(tipologia: "PRIMO SALDO' OR '1'='1"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_DueRichiesteInParallelo_ShouldPassarneUnaSola()
    {
        var risposte = await Task.WhenAll(Genera(Body()), Genera(Body()));
        var codici = risposte.Select(r => r.StatusCode).OrderBy(c => (int)c).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(codici, Is.EqualTo(new[] { HttpStatusCode.Accepted, HttpStatusCode.Conflict }));
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1), "Un solo batch: il lock ferma la seconda.");
        });
    }
}
