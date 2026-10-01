using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte api/v2/pagopa/psps* (anagrafica PSP, prodotto pagoPA): anni, trimestri, ricerca per nome,
/// griglia paginata e download Excel. Leggono [ppa].[Contracts] direttamente, senza vista.
///
/// Seed (tests/Data/ppa_contracts.sql): trimestre più recente 2026_1 con T01…T05; T01 anche in
/// 2025_4, T06 solo in 2025_4; "Banca" nel nome di T01, T02, T06.
/// </summary>
public class PSPHttpTests
{
    private const string Base = "/api/v2/pagopa/psps";

    private ApiTestFactory _factory = null!;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    /// <summary>
    /// Salta i test se il container non è raggiungibile o se ppa.Contracts non è nel seed.
    /// </summary>
    [SetUp]
    public void CheckDb()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "ppa.Contracts");
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
    /// Gli anni sono ricavati dai trimestri: distinti, dal più recente.
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
    /// I trimestri dell'anno richiesto, con etichetta "Qn", in ordine crescente.
    /// </summary>
    [Test]
    public async Task Quarters_PerAnno_ShouldRestituireITrimestriConEtichetta()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "2025" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(json, "value"), Is.EqualTo(new[] { "2025_4" }));
            Assert.That(Valori(json, "quarter"), Is.EqualTo(new[] { "Q4" }));
        });
    }

    /// <summary>Un anno senza contratti risponde 404.</summary>
    [Test]
    public async Task Quarters_AnnoSenzaDati_ShouldReturn404()
    {
        var resp = await Post($"{Base}/quarters", """{ "year": "1999" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- name ---

    /// <summary>
    /// La ricerca per nome cerca nel trimestre più recente: T06 ha il nome giusto ma è solo in 2025_4.
    /// </summary>
    [Test]
    public async Task Name_SenzaTrimestre_ShouldCercareNelPiuRecente()
    {
        var resp = await Post($"{Base}/name", """{ "name": "Banca" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori(await Json(resp), "contractId"), Is.EqualTo(new[] { "PSP-T01", "PSP-T02" }));
    }

    /// <summary>Con i trimestri indicati cerca solo in quelli.</summary>
    [Test]
    public async Task Name_ConTrimestre_ShouldCercareSoloLi()
    {
        var resp = await Post($"{Base}/name", """{ "name": "Banca", "quarters": ["2025_4"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori(await Json(resp), "contractId"), Is.EqualTo(new[] { "PSP-T01", "PSP-T06" }));
    }

    /// <summary>Un nome che non corrisponde a nessun contratto risponde 404.</summary>
    [Test]
    public async Task Name_NessunaCorrispondenza_ShouldReturn404()
    {
        var resp = await Post($"{Base}/name", """{ "name": "nome-che-non-esiste" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- grid ---

    /// <summary>
    /// Senza filtri la griglia mostra il trimestre più recente, ordinato per contract_id, con il
    /// conteggio totale. La lista viaggia nella proprietà "psPs" (camelCase di PSPs).
    /// </summary>
    [Test]
    public async Task Grid_SenzaFiltri_ShouldRestituireIlTrimestrePiuRecente()
    {
        var resp = await Post(Base, "{}", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(Valori(json.GetProperty("psPs"), "contractId"),
                Is.EqualTo(new[] { "PSP-T01", "PSP-T02", "PSP-T03", "PSP-T04", "PSP-T05" }));
        });
    }

    /// <summary>La seconda pagina restituisce le sole sue righe; il conteggio resta sul totale.</summary>
    [Test]
    public async Task Grid_SecondaPagina_ShouldRestituireLaPaginaEIlCountTotale()
    {
        var resp = await Post(Base, "{}", "&page=2&pageSize=2");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(Valori(json.GetProperty("psPs"), "contractId"), Is.EqualTo(new[] { "PSP-T03", "PSP-T04" }));
        });
    }

    /// <summary>I filtri si applicano insieme (AND): membership e ABI.</summary>
    [Test]
    public async Task Grid_FiltriCombinati_ShouldIntersecare()
    {
        var resp = await Post(Base, """{ "membershipId": "grp-2", "abi": "09999" }""", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori((await Json(resp)).GetProperty("psPs"), "contractId"), Is.EqualTo(new[] { "PSP-T04" }));
    }

    /// <summary>Un filtro che non trova contratti risponde 404.</summary>
    [Test]
    public async Task Grid_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post(Base, """{ "quarters": ["1999_1"] }""", "&page=1&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Senza autenticazione la griglia risponde 401.</summary>
    [Test]
    public async Task Grid_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(Base, "{}", "&page=1&pageSize=10", ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>OPERATOR è ammesso: le rotte sono di sola lettura.</summary>
    [Test]
    public async Task Grid_Operator_ShouldReturn200()
    {
        var resp = await Post(Base, "{}", "&page=1&pageSize=10", ruolo: Ruolo.OPERATOR);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // --- document ---

    /// <summary>
    /// Il download restituisce un Excel con tutti i contratti del filtro, senza paginazione: senza
    /// filtri, quelli del trimestre più recente.
    /// </summary>
    [Test]
    public async Task Document_ShouldRestituireUnExcelConTuttiIContrattiDelFiltro()
    {
        var resp = await Post($"{Base}/document", "{}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));

        // l'xlsx è uno zip Open XML: i testi delle celle sono inline nei fogli
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        var testo = string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));

        Assert.Multiple(() =>
        {
            foreach (var id in new[] { "PSP-T01", "PSP-T02", "PSP-T03", "PSP-T04", "PSP-T05" })
                Assert.That(testo, Does.Contain(id), id);
            Assert.That(testo, Does.Not.Contain("PSP-T06"), "T06 è solo in 2025_4, fuori dal default");
        });
    }

    /// <summary>Un filtro che non trova contratti risponde 404 e non produce alcun file.</summary>
    [Test]
    public async Task Document_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/document", """{ "quarters": ["1999_1"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
