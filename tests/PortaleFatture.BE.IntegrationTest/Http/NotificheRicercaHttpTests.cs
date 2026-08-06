using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP su POST api/notifiche/pagopa — ricerca notifiche lato admin. Copre BE-SMOKE-03 del
/// testbook backend.
///
/// L'area Notifiche non usa viste: la query di NotificaSQLBuilder e' una catena di 6 JOIN su tabelle
/// (Notifiche + Enti + Contratti + Notifiche_CodiceOggetto + Contestazioni + FlagContestazione +
/// TipoContestazione), quindi questi test verificano soprattutto che la catena regga e che una
/// notifica NON contestata sopravviva all'INNER JOIN su FlagContestazione (il ramo ISNULL(...,1)).
///
/// Seed dedicato: ente1 / 2026 / mese 3, tre notifiche (EVT-3001 analogica non contestata,
/// EVT-3002 analogica contestata con codice oggetto, EVT-3003 digitale gia' fatturata).
/// Dettagli e trappole in tests/Data/notifiche.sql.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class NotificheRicercaHttpTests
{
    private const string Rotta = "/api/notifiche/pagopa?page=1&pageSize=50";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb() => TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);

    private async Task<HttpResponseMessage> Post(string body, string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        // WithNonce aggiunge ?nonce=... : la rotta ha gia' una query string, quindi il nonce va unito
        // manualmente con &, altrimenti si otterrebbe un secondo '?' e il middleware non lo troverebbe.
        var rotta = $"{Rotta}&nonce={Uri.EscapeDataString(_factory.Nonce())}";
        var resp = await client.PostAsync(rotta, content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task Ricerca_PeriodoConDati_ShouldReturn200_ConTreNotifiche()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 3 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.That(root.GetProperty("count").GetInt32(), Is.EqualTo(3),
            "Le tre notifiche del seed, comprese le due NON contestate: se il ramo ISNULL dell'INNER JOIN "
            + "su FlagContestazione si rompesse, resterebbe solo la contestata.");

        var notifiche = root.GetProperty("notifiche").EnumerateArray().ToList();
        Assert.That(notifiche, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Ricerca_NotificaContestata_ShouldEsporreTipoContestazioneECodiceOggetto()
    {
        // EVT-3002 e' l'unica con contestazione (stato 3, tipo 1) e con riga in Notifiche_CodiceOggetto:
        // verifica insieme i tre JOIN opzionali della catena.
        var resp = await Post("""{ "anno": 2026, "mese": 3 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("Contestata Ente"), "FlagContestazione della notifica contestata.");
            Assert.That(body, Does.Contain("Mancato recapito"), "TipoContestazione risolto dal LEFT JOIN.");
            Assert.That(body, Does.Contain("CODOGG-3002"), "CodiceOggetto dal LEFT JOIN su Notifiche_CodiceOggetto.");
            Assert.That(body, Does.Contain("Non Contestata"), "Le altre due restano nel default (ISNULL -> 1).");
        });
    }

    [Test]
    public async Task Ricerca_PeriodoVuoto_ShouldReturn404()
    {
        // Contratto attuale dell'endpoint: lista vuota -> 404, non 200 con array vuoto.
        // Stessa convenzione di api/fatture (v. FattureRicercaHttpTests).
        var resp = await Post("""{ "anno": 1999, "mese": 1 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Ricerca_FiltroPerStatoContestazione_ShouldRestituireSoloLaContestata()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 3, "statoContestazione": [3] }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(1),
            "Filtrando su 'Contestata Ente' deve restare solo EVT-3002.");
    }

    [Test]
    public async Task Ricerca_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 3 }""", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
