using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte api/appio/contracts* (PF-908): filtri Anno / Trimestre / Nome Ente, griglia e download Excel
/// dei contratti APP IO. Leggono [be].[vwAppioContracts].
///
/// Seed (tests/Data/appio.sql): trimestre più recente 2026_2 con C1, C2, C3; C1 anche in 2026_1 e
/// 2025_4; C4 solo in 2025_4. "AppIO Test" è nel nome di C1, C2, C4 ma non di C3.
/// </summary>
public class AppIoContrattiHttpTests
{
    private const string Base = "/api/appio/contracts";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContracts");
    }

    /// <summary>
    /// POST con il ruolo indicato (null = anonimo) e nonce valido; query = parametri aggiuntivi in query string.
    /// </summary>
    private async Task<HttpResponseMessage> Post(string rotta, string body, string query = "", string? ruolo = Ruolo.ADMIN)
    {
        var client = _factory.CreateClientAs(ruolo);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta) + query, content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    /// <summary>
    /// Corpo della risposta come JSON.
    /// </summary>
    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    /// <summary>
    /// Valori di una proprietà stringa per ogni elemento di un array JSON.
    /// </summary>
    private static List<string> Valori(JsonElement array, string proprieta)
        => array.EnumerateArray().Select(x => x.GetProperty(proprieta).GetString()!).ToList();

    // --- years ---

    /// <summary>
    /// Il filtro "Anno" restituisce gli anni distinti ricavati dai trimestri, dal più recente.
    /// </summary>
    [Test]
    public async Task Years_ShouldRestituireGliAnniDistintiDalPiuRecente()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(_factory.WithNonce($"{Base}/years"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var anni = (await Json(resp)).EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.That(anni, Is.EqualTo(new[] { "2026", "2025" }));
    }

    // --- quarters ---

    /// <summary>
    /// Il filtro "Trimestre" restituisce i trimestri dell'anno richiesto, con etichetta "Qn", in ordine crescente.
    /// </summary>
    [Test]
    public async Task Quarters_PerAnno_ShouldRestituireITrimestriInOrdine()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "2026" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(json, "value"), Is.EqualTo(new[] { "2026_1", "2026_2" }));
            Assert.That(Valori(json, "quarter"), Is.EqualTo(new[] { "Q1", "Q2" }));
        });
    }

    /// <summary>
    /// Un anno senza contratti risponde 404.
    /// </summary>
    [Test]
    public async Task Quarters_AnnoSenzaDati_ShouldReturn404()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "1999" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- name ---

    /// <summary>
    /// Senza trimestri la ricerca per nome cerca nel trimestre più recente e ordina per nome.
    /// </summary>
    [Test]
    public async Task Name_SenzaTrimestre_ShouldCercareNelPiuRecente_OrdinatoPerNome()
    {
        var resp = await Post($"{Base}/name", """{ "name": "AppIO Test" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        // C4 ha il nome giusto ma esiste solo in 2025_4: fuori dal default
        Assert.That(Valori(await Json(resp), "contractId"), Is.EqualTo(new[] { "APPIO-C1", "APPIO-C2" }));
    }

    /// <summary>
    /// Con i trimestri indicati la ricerca per nome cerca solo in quelli.
    /// </summary>
    [Test]
    public async Task Name_ConTrimestre_ShouldCercareSoloLi()
    {
        var resp = await Post($"{Base}/name", """{ "name": "AppIO Test", "quarters": ["2025_4"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori(await Json(resp), "contractId"), Is.EqualTo(new[] { "APPIO-C1", "APPIO-C4" }));
    }

    /// <summary>
    /// Un nome che non corrisponde a nessun contratto risponde 404.
    /// </summary>
    [Test]
    public async Task Name_NessunaCorrispondenza_ShouldReturn404()
    {
        var resp = await Post($"{Base}/name", """{ "name": "nome-che-non-esiste" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- grid ---

    /// <summary>
    /// Senza filtri la griglia mostra i contratti del trimestre più recente, ordinati per contract_id, con il conteggio totale.
    /// </summary>
    [Test]
    public async Task Grid_SenzaFiltri_ShouldRestituireIlTrimestrePiuRecente()
    {
        var resp = await Post(Base, "{}", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(3));
            Assert.That(Valori(json.GetProperty("contratti"), "contractId"),
                Is.EqualTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3" }));
        });
    }

    /// <summary>
    /// La seconda pagina restituisce le sole righe della pagina, ma il conteggio resta quello totale.
    /// </summary>
    [Test]
    public async Task Grid_SecondaPagina_ShouldRestituireLaPaginaEIlCountTotale()
    {
        var resp = await Post(Base, "{}", "&page=2&pageSize=2");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(3));
            Assert.That(Valori(json.GetProperty("contratti"), "contractId"), Is.EqualTo(new[] { "APPIO-C3" }));
        });
    }

    /// <summary>
    /// I filtri per contratto e per trimestre si applicano insieme (AND).
    /// </summary>
    [Test]
    public async Task Grid_FiltroContrattiETrimestri_ShouldIntersecare()
    {
        var resp = await Post(Base, """{ "contractIds": ["APPIO-C1"], "quarters": ["2025_4", "2026_1"] }""", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(2));
            Assert.That(Valori(json.GetProperty("contratti"), "yearQuarter"), Is.EquivalentTo(new[] { "2025_4", "2026_1" }));
        });
    }

    /// <summary>
    /// Un filtro che non trova contratti risponde 404.
    /// </summary>
    [Test]
    public async Task Grid_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post(Base, """{ "quarters": ["1999_1"] }""", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Senza autenticazione la griglia risponde 401.
    /// </summary>
    [Test]
    public async Task Grid_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(Base, "{}", "&page=1&pageSize=10", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- download ---

    /// <summary>
    /// Il download restituisce un Excel con tutti i contratti del filtro, senza paginazione: senza filtri, quelli del trimestre più recente.
    /// </summary>
    [Test]
    public async Task Download_ShouldRestituireUnExcelConTuttiIContrattiDelFiltro_SenzaPaginazione()
    {
        var resp = await Post($"{Base}/download", "{}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));

        // l'xlsx è uno zip Open XML: i testi delle celle sono inline nei fogli
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        var testo = string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));

        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("APPIO-C1"));
            Assert.That(testo, Does.Contain("APPIO-C2"));
            Assert.That(testo, Does.Contain("APPIO-C3"));
            Assert.That(testo, Does.Not.Contain("APPIO-C4"), "C4 è solo in 2025_4, fuori dal default");
        });
    }

    /// <summary>
    /// Un filtro che non trova contratti risponde 404 e non produce alcun file.
    /// </summary>
    [Test]
    public async Task Download_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/download", """{ "quarters": ["1999_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
