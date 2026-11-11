using System.Net;
using PortaleFatture.BE.IntegrationTest.InvioSap;
using static PortaleFatture.BE.IntegrationTest.Http.InvioSapHttp;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Invio a SAP e Gestione Fatture (viste 1.1 di DATA, 11/11/2026): una fattura posticipata (stato 0) o eliminata
/// (stato 3) in <c>cfg.GestioneFatture</c> non compare in griglia ne' nel dettaglio, e il Genera non la invia.
/// Ripristinata (1) e cancellata (2) restano. La regola deve essere la stessa in tre punti: griglia, dettaglio e
/// INSERT del primo invio. Se uno dei tre diverge, il numero del rigo non coincide con le fatture inserite e il
/// Genera risponde 409 GRUPPO_CAMBIATO, oppure invia a SAP una fattura che l'utente non vede.
/// Le righe di Gestione Fatture sono create dal test e tolte da <see cref="InvioSapTestData.Pulisci"/>.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapGestioneFattureHttpTests
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
        InvioSapTestData.RilasciaLockComeData();
    }

    [TearDown]
    public void Cleanup() => InvioSapTestData.Pulisci();

    private Task<HttpResponseMessage> Genera(string body) => Put(_factory, InvioSapTestData.RottaGenera, body);

    private static string PrimoInvio() =>
        BodyGenera(Anno, InvioSapTestData.MesePrimoInvio, InvioSapTestData.TipologiaPrimoInvio, "Primo invio con Gestione Fatture");

    private const string SelezionaTutto =
        """{ "anno": 2003, "mese": 1, "tipologiaFattura": "PRIMO SALDO", "nota": "Tutto", "allSelected": true, "numeroDocumentoStorno": "NC-1" }""";

    private static int? NumeroInGriglia(int mese, int stato) =>
        InvioSapTestData.Scalar($@"SELECT NumeroFatture FROM {InvioSapTestData.VistaGriglia}
            WHERE Anno = 2003 AND Mese = @m AND TipologiaFattura = 'PRIMO SALDO' AND StatoFattura = @s", ("@m", mese), ("@s", stato)) as int?;

    private static List<long> NelDettaglio(int mese) =>
        InvioSapTestData.Lista<long>($@"SELECT IdFattura FROM {InvioSapTestData.VistaDettaglio}
            WHERE AnnoRiferimento = 2003 AND MeseRiferimento = @m AND FkTipologiaFattura = 'PRIMO SALDO' ORDER BY IdFattura", ("@m", mese));

    private static async Task<List<long>> IdFatture(HttpResponseMessage resp) =>
        (await Json(resp)).GetProperty("idFatture").EnumerateArray().Select(e => e.GetInt64()).ToList();

    // =============================================================================================
    // Primo invio
    // =============================================================================================

    [TestCase(0, TestName = "PrimoInvio_ShouldEscludere(posticipata)")]
    [TestCase(3, TestName = "PrimoInvio_ShouldEscludere(eliminata)")]
    public async Task PrimoInvio_ShouldEscludere(int stato)
    {
        InvioSapTestData.MettiInGestioneFatture(9505, stato);
        Assert.That(NumeroInGriglia(2, 0), Is.EqualTo(1), "Il rigo da inviare conta solo 9506.");
        Assert.That(NelDettaglio(2), Has.None.EqualTo(9505L), "Il dettaglio non la mostra.");

        var resp = await Genera(PrimoInvio());

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        var inviate = await IdFatture(resp);
        Assert.Multiple(() =>
        {
            Assert.That(inviate, Is.EqualTo(new[] { 9506L }), "Parte solo la fattura che l'utente vede.");
            Assert.That(InvioSapTestData.FatturaInviata(9505), Is.False, "La fattura esclusa resta da inviare.");
            Assert.That(InvioSapTestData.FatturaInviata(9506), Is.Null, "Quella del batch e' in elaborazione.");
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.Dettaglio} WHERE FkIdFattura = 9505"), Is.EqualTo(0));
        });
    }

    [TestCase(1, TestName = "PrimoInvio_ShouldIncludere(ripristinata)")]
    [TestCase(2, TestName = "PrimoInvio_ShouldIncludere(cancellata)")]
    public async Task PrimoInvio_ShouldIncludere(int stato)
    {
        InvioSapTestData.MettiInGestioneFatture(9505, stato);
        Assert.That(NumeroInGriglia(2, 0), Is.EqualTo(2));

        var resp = await Genera(PrimoInvio());

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        Assert.That(await IdFatture(resp), Is.EqualTo(new[] { 9505L, 9506L }));
    }

    [Test]
    public async Task PrimoInvio_TutteEscluse_ShouldEssere404_SenzaScritture()
    {
        InvioSapTestData.MettiInGestioneFatture(9505, 0);
        InvioSapTestData.MettiInGestioneFatture(9506, 3);
        var prima = InvioSapTestData.FotografiaTabelle();
        Assert.That(NumeroInGriglia(2, 0), Is.Null, "Il rigo da inviare non esiste piu'.");

        var resp = await Genera(PrimoInvio());

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima));
            Assert.That(FakeSynapseService.Instance.Chiamate, Is.Empty);
        });
    }

    // =============================================================================================
    // Re-invio
    // =============================================================================================

    [TestCase(0, TestName = "ReInvio_Elenco_ConFatturaEsclusa_ShouldEssere404(posticipata)")]
    [TestCase(3, TestName = "ReInvio_Elenco_ConFatturaEsclusa_ShouldEssere404(eliminata)")]
    public async Task ReInvio_Elenco_ConFatturaEsclusa_ShouldEssere404(int stato)
    {
        InvioSapTestData.MettiInGestioneFatture(9503, stato);
        var prima = InvioSapTestData.FotografiaTabelle();

        var resp = await Genera(BodyGenera(Anno, 1, "PRIMO SALDO", "Re-invio", [9502, 9503], "NC-1"));

        Assert.Multiple(() =>
        {
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "9503 non e' piu' nel dettaglio: per il re-invio non esiste.");
            Assert.That(InvioSapTestData.BatchDeiTest(), Is.Empty);
            Assert.That(InvioSapTestData.FotografiaTabelle(), Is.EqualTo(prima));
        });
    }

    [TestCase(0, TestName = "ReInvio_SelezionaTutto_ShouldSaltareLaFatturaEsclusa(posticipata)")]
    [TestCase(3, TestName = "ReInvio_SelezionaTutto_ShouldSaltareLaFatturaEsclusa(eliminata)")]
    public async Task ReInvio_SelezionaTutto_ShouldSaltareLaFatturaEsclusa(int stato)
    {
        InvioSapTestData.MettiInGestioneFatture(9503, stato);
        Assert.That(NumeroInGriglia(1, 1), Is.EqualTo(1), "Il rigo inviate conta solo 9502.");

        var resp = await Genera(SelezionaTutto);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Accepted), await resp.Content.ReadAsStringAsync());
        var inviate = await IdFatture(resp);
        Assert.Multiple(() =>
        {
            Assert.That(inviate, Is.EqualTo(new[] { 9502L }));
            Assert.That(InvioSapTestData.FatturaInviata(9503), Is.True, "La fattura esclusa non viene toccata.");
        });
    }

    // =============================================================================================
    // Letture
    // =============================================================================================

    [Test]
    public async Task Dettaglio_ShouldNonMostrareLaFatturaPosticipata()
    {
        InvioSapTestData.MettiInGestioneFatture(9503, 0);

        var corpo = await Json(await Post(_factory, InvioSapTestData.RottaDettaglio,
            """{ "annoRiferimento": 2003, "meseRiferimento": 1, "tipologiaFattura": "PRIMO SALDO", "statoInvio": "INVIATE" }"""));

        var ids = corpo.GetProperty("fatture").EnumerateArray().Select(f => f.GetProperty("idFattura").GetInt64()).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(ids, Is.EqualTo(new[] { 9502L }));
            Assert.That(corpo.GetProperty("count").GetInt32(), Is.EqualTo(NumeroInGriglia(1, 1)), "Dettaglio e griglia contano le stesse fatture.");
        });
    }
}
