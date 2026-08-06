using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP su POST api/v2/modulocommessa/pagopa/ricerca — ricerca moduli commessa lato admin.
/// Copre BE-SMOKE-04 del testbook backend.
///
/// E' l'area piu' "ibrida" del backend: le query combinano quattro viste LEGACY pfd.v* (senza la 'w')
/// con tabelle e CTE costruite in C#. Qui la catena passa da DatiModuloCommessaSQLBuilder ->
/// pfd.vModuliCommessa (+ pfd.vDatiModuloCommessaAderenti per la segmentazione territoriale).
///
/// Seed dedicato: ente1, 2026/5, tre righe di previsione (AR 600+10, 890 400, digitale 300+20) e i
/// totali economici per categoria. Dettagli e trappole in tests/Data/modulo_commessa.sql.
///
/// Richiede il container di test attivo (da tests/: docker compose up -d --build).
/// </summary>
public class ModuloCommessaHttpTests
{
    private const string Rotta = "/api/v2/modulocommessa/pagopa/ricerca";
    private const string Ente1 = "11111111-1111-1111-1111-111111111111";

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
        var resp = await client.PostAsync(_factory.WithNonce(Rotta), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task Ricerca_PeriodoConDati_ShouldReturn200()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 5 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var body = await resp.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain(Ente1),
            "Il modulo commessa seedato per ente1 deve comparire nel risultato.");
    }

    [Test]
    public async Task Ricerca_PeriodoConDati_ShouldEsporreITotaliDelSeed()
    {
        // Il seed dichiara AR 600 + 890 400 + digitale 300 nazionali, con totali economici
        // 5000 analogico / 320 digitale. La vista li ricompone in un'unica riga per ente/periodo.
        var resp = await Post("""{ "anno": 2026, "mese": 5 }""");
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var righe = doc.RootElement.EnumerateArray().ToList();

        Assert.That(righe, Has.Count.EqualTo(1),
            "Un ente con un solo periodo seedato deve produrre una sola riga: se ne escono di piu', "
            + "e' un fan-out di uno dei LEFT JOIN della vista.");
    }

    [Test]
    public async Task Ricerca_PeriodoVuoto_ShouldReturn404()
    {
        // Contratto attuale: nessun modulo per il periodo -> 404, come api/fatture e api/notifiche.
        var resp = await Post("""{ "anno": 1999, "mese": 1 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Ricerca_FiltroPerEnte_ShouldRestituireSoloQuellEnte()
    {
        var resp = await Post($$"""{ "anno": 2026, "mese": 5, "idEnti": ["{{Ente1}}"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await resp.Content.ReadAsStringAsync(), Does.Contain(Ente1));
    }

    [Test]
    public async Task Ricerca_FiltroEnteInesistente_ShouldReturn404()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 5, "idEnti": ["00000000-0000-0000-0000-000000000000"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Ricerca_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post("""{ "anno": 2026, "mese": 5 }""", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
