using System.Net;
using System.Text.Json;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Genera con <c>statoInvio</c> (11/11/2026): la selezione vale anche per le fatture da inviare. Con
/// <c>DA_INVIARE</c> l'elenco o il "seleziona tutto" producono un primo invio di una parte del rigo (4 → 5, senza
/// storno); con <c>INVIATE</c> un re-invio; senza, il contratto precedente. Rigo usato: PRIMO SALDO 2003/2, con
/// 9505 e 9506 da inviare, 9507 inviata e 9508 in elaborazione.
/// Ogni rifiuto e' controllato anche sulle scritture: nessun batch, nessuna fattura toccata, pipeline non chiamata.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapPrimoInvioSelezioneHttpTests
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

    private static string Body(string? stato = "DA_INVIARE", bool? tutto = null, long[]? ids = null, long[]? escluse = null,
        string? storno = null, int mese = 2, string tipologia = "PRIMO SALDO") =>
        JsonSerializer.Serialize(new
        {
            anno = 2003, mese, tipologiaFattura = tipologia, nota = "Selezione nel primo invio", statoInvio = stato,
            allSelected = tutto, idFatture = ids, idFattureEscluse = escluse, numeroDocumentoStorno = storno
        }, Web);

    private static async Task<List<long>> IdFatture(HttpResponseMessage resp) =>
        (await Json(resp)).GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()).ToList();

    /// <summary>Stato precedente, stato e storno delle righe di dettaglio dell'unico batch creato dal test.</summary>
    private static List<string> RigheDelBatch() =>
        InvioSapTestData.Lista<string>($@"SELECT CONCAT(d.FkIdFattura, ':', d.FkStatoPrecedente, '>', d.FkStato, ':', ISNULL(d.NumeroDocStorno, 'NULL'))
            FROM {InvioSapTestData.Dettaglio} d JOIN {InvioSapTestData.Registro} r ON r.BatchId = d.FkBatchId
            WHERE r.IdUtente = @u ORDER BY d.FkIdFattura", ("@u", InvioSapTestData.UtenteTest));

    private static void NessunaScrittura(InvioSapTestData.Fotografia prima)
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty, "Nessun batch.");
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima), "Righe preesistenti invariate.");
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty, "Pipeline non chiamata.");
        });
    }

    // =============================================================================================
    // Selezione nel primo invio
    // =============================================================================================

    [Test]
    public async Task Elenco_ShouldInviareSoloLeFattureScelte()
    {
        var resp = await Genera(Body(ids: [9505]));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        var inviate = await IdFatture(resp);
        Assert.Multiple(() =>
        {
            Assert.That(inviate, Is.EqualTo(new[] { 9505L }));
            Assert.That(RigheDelBatch(), Is.EqualTo(new[] { "9505:4>5:NULL" }), "Primo invio: da non inviata a inviata, senza storno.");
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.Null, "In elaborazione.");
            Assert.That(InvioSapTestData.FatturaInviata(9506), Is.False, "Non scelta: resta da inviare.");
            Assert.That(InvioSapTestData.FatturaInviata(9507), Is.True);
            Assert.That(InvioSapTestData.BatchDeiTest(), Has.Count.EqualTo(1));
            Assert.That(FakeSynapseService.Instance.Chiamate, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Elenco_DopoLaChiusuraDelBatch_ShouldPoterInviareLeRimanenti()
    {
        var primo = await Genera(Body(ids: [9505]));
        Assert.That(primo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        InvioSapTestData.ChiudiBatchComeData(InvioSapTestData.BatchDeiTest().Single());

        var secondo = await Genera(Body(tutto: true));

        Assert.That(secondo.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await secondo.Content.ReadAsStringAsync());
        Assert.That(await IdFatture(secondo), Is.EqualTo(new[] { 9506L }), "9505 ora e' inviata: il seleziona tutto prende la sola rimasta.");
    }

    [TestCase(null, new long[] { 9505, 9506 }, TestName = "SelezionaTutto_ShouldInviareLeFattureDaInviare(nessuna esclusa)")]
    [TestCase(new long[] { 9506 }, new long[] { 9505 }, TestName = "SelezionaTutto_ShouldInviareLeFattureDaInviare(una esclusa)")]
    [TestCase(new long[] { 9507 }, new long[] { 9505, 9506 }, TestName = "SelezionaTutto_ShouldInviareLeFattureDaInviare(esclusa una inviata: non cambia nulla)")]
    public async Task SelezionaTutto_ShouldInviareLeFattureDaInviare(long[]? escluse, long[] attese)
    {
        var resp = await Genera(Body(tutto: true, escluse: escluse));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        var inviate = await IdFatture(resp);
        Assert.Multiple(() =>
        {
            Assert.That(inviate, Is.EqualTo(attese));
            Assert.That(RigheDelBatch(), Is.EqualTo(attese.Select(id => $"{id}:4>5:NULL")));
            Assert.That(InvioSapTestData.FatturaInviata(9507), Is.True, "L'inviata del rigo non entra nel primo invio.");
            Assert.That(InvioSapTestData.FatturaInviata(9508), Is.Null, "Quella in elaborazione non viene toccata.");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkIdFattura IN (9507, 9508) AND FkBatchId > {InvioSapTestData.UltimoBatchSeed}"), Is.EqualTo(0));
        });
    }

    [Test]
    public async Task SelezionaTutto_ConUnaPosticipata_ShouldSaltarla()
    {
        InvioSapTestData.MettiInGestioneFatture(9505, 0);
        var resp = await Genera(Body(tutto: true));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        Assert.That(await IdFatture(resp), Is.EqualTo(new[] { 9506L }));
    }

    [TestCase("DA_INVIARE", TestName = "SenzaSelezione_ShouldInviareIlRigoIntero(DA_INVIARE)")]
    [TestCase(" da_inviare ", TestName = "SenzaSelezione_ShouldInviareIlRigoIntero(minuscolo con spazi)")]
    [TestCase(null, TestName = "SenzaSelezione_ShouldInviareIlRigoIntero(statoInvio assente: contratto precedente)")]
    public async Task SenzaSelezione_ShouldInviareIlRigoIntero(string? stato)
    {
        var resp = await Genera(Body(stato: stato));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        Assert.That(await IdFatture(resp), Is.EqualTo(new[] { 9505L, 9506L }));
    }

    // =============================================================================================
    // Rifiuti, senza scritture
    // =============================================================================================

    [TestCase(9507L, "ancora da inviare", TestName = "Elenco_FatturaNonDaInviare_ShouldEssere400(inviata)")]
    [TestCase(9508L, "ancora da inviare", TestName = "Elenco_FatturaNonDaInviare_ShouldEssere400(in elaborazione)")]
    [TestCase(9509L, "anno, al mese e alla tipologia", TestName = "Elenco_FatturaNonDaInviare_ShouldEssere400(altra tipologia)")]
    public async Task Elenco_FatturaNonDaInviare_ShouldEssere400(long id, string messaggio)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(ids: [9505, id]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain(messaggio));
        });
        NessunaScrittura(prima);
    }

    [TestCase(987654321L, TestName = "Elenco_FatturaAssenteDalDettaglio_ShouldEssere404(inesistente)")]
    [TestCase(9510L, TestName = "Elenco_FatturaAssenteDalDettaglio_ShouldEssere404(ente non in anagrafica)")]
    public async Task Elenco_FatturaAssenteDalDettaglio_ShouldEssere404(long id)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(ids: [id], mese: id == 9510 ? 3 : 2, tipologia: id == 9510 ? "ACCONTO" : "PRIMO SALDO"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task Elenco_Posticipata_ShouldEssere404()
    {
        InvioSapTestData.MettiInGestioneFatture(9505, 0);
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(ids: [9505, 9506]));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [TestCase(false, TestName = "ConStorno_ShouldEssere400(elenco)")]
    [TestCase(true, TestName = "ConStorno_ShouldEssere400(seleziona tutto)")]
    public async Task ConStorno_ShouldEssere400(bool tutto)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(tutto: tutto ? true : null, ids: tutto ? null : [9505], storno: "NC-1"));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("non e' ammesso nel primo invio"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task ElencoVuoto_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(ids: []));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_TutteEscluse_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(tutto: true, escluse: [9505, 9506]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("almeno una fattura"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task SelezionaTutto_RigoSenzaFattureDaInviare_ShouldEssere404()
    {
        // PRIMO SALDO 2003/1 ha solo fatture inviate.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(tutto: true, mese: 1));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        NessunaScrittura(prima);
    }

    [TestCase("REINVIATE")]
    [TestCase("IN_ELABORAZIONE")]
    [TestCase("TUTTE")]
    public async Task StatoNonValido_ShouldEssere400(string stato)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(stato: stato, ids: [9505]));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("Stato invio non valido"));
        });
        NessunaScrittura(prima);
    }

    // =============================================================================================
    // statoInvio = INVIATE: il re-invio, dichiarato
    // =============================================================================================

    [Test]
    public async Task Inviate_Elenco_ShouldEssereUnReInvioConStorno()
    {
        var resp = await Genera(Body(stato: "INVIATE", ids: [9507], storno: "NC-9"));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        Assert.That(RigheDelBatch(), Is.EqualTo(new[] { "9507:5>5:NC-9" }));
    }

    [TestCase(false, TestName = "Inviate_SenzaStorno_ShouldEssere400(elenco)")]
    [TestCase(true, TestName = "Inviate_SenzaStorno_ShouldEssere400(seleziona tutto)")]
    public async Task Inviate_SenzaStorno_ShouldEssere400(bool tutto)
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(stato: "INVIATE", tutto: tutto ? true : null, ids: tutto ? null : [9507]));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        NessunaScrittura(prima);
    }

    [Test]
    public async Task Inviate_SenzaSelezione_ShouldEssere400_ENonUnPrimoInvio()
    {
        // Senza elenco e senza seleziona tutto, INVIATE non ricade nel primo invio del rigo intero.
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(stato: "INVIATE", storno: "NC-9"));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("almeno una fattura"));
        });
        NessunaScrittura(prima);
    }

    [Test]
    public async Task Inviate_ElencoConFatturaDaInviare_ShouldEssere400()
    {
        var prima = InvioSapTestData.FotografiaTabelle();
        var resp = await Genera(Body(stato: "INVIATE", ids: [9505], storno: "NC-9"));
        var testo = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(testo, Does.Contain("solo fatture gia' inviate"));
        });
        NessunaScrittura(prima);
    }
}
