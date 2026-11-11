using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Rotte api/appio/contracts* (modello dati dell'11/11/2026, PF-1016): filtri Tipo Ente e Nome Ente,
/// griglia e download Excel dell'anagrafica APP IO.
///
/// Seed (tests/Data/appio.sql): OB1 (ENTE-1, "Comune Alfa AppIO Test", PA), OB2 (ENTE-2, PA),
/// OB3 (ENTE-3, "Regione Gamma", PA, SUSPENDED), OB4 (ENTE-4, omonimo di OB1, GSP), OB5 (ENTE-5,
/// nome e tipo NULL).
/// </summary>
[Category("AppIO")]
public class AppIoContrattiHttpTests
{
    private const string Base = "/api/appio/contracts";
    private const string Paginazione = "&page=1&pageSize=10";

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
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContractsFiltroNome");
        TestDb.SkipSeOggettoAssente(LocalTestDb.ConnectionString, "be.vwAppioContractsFiltroTipoEnte");
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

    private static async Task<JsonElement> Json(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    private static List<string?> Valori(JsonElement array, string proprieta)
        => array.EnumerateArray().Select(x => x.GetProperty(proprieta).GetString()).ToList();

    /// <summary>Testo dei fogli di un xlsx (le celle sono inline nei fogli).</summary>
    private static async Task<string> TestoExcel(HttpResponseMessage resp)
    {
        using var zip = new ZipArchive(await resp.Content.ReadAsStreamAsync());
        return string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));
    }

    // --- rotte rimosse ---

    /// <summary>
    /// Con il nuovo modello il contratto non è più storicizzato per trimestre: years e quarters
    /// dell'anagrafica non esistono più.
    /// </summary>
    [Test]
    public async Task YearsEQuarters_ShouldNonEsisterePiu()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var years = await client.GetAsync(_factory.WithNonce($"{Base}/years"));
        var quarters = await Post($"{Base}/quarters", """{ "year": "2026" }""");

        Assert.Multiple(() =>
        {
            Assert.That(years.StatusCode, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.MethodNotAllowed));
            Assert.That(quarters.StatusCode, Is.EqualTo(HttpStatusCode.NotFound).Or.EqualTo(HttpStatusCode.MethodNotAllowed));
        });
    }

    // --- tipoente ---

    /// <summary>
    /// Il filtro "Tipo Ente" restituisce i tipi distinti in ordine alfabetico, anche all'OPERATOR.
    /// </summary>
    [Test]
    public async Task TipoEnte_ShouldRestituireITipiDistinti()
    {
        var client = _factory.CreateClientAs(Ruolo.OPERATOR);
        var resp = await client.GetAsync(_factory.WithNonce($"{Base}/tipoente"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Json(resp)).EnumerateArray().Select(x => x.GetString()), Is.EqualTo(new[] { "GSP", "PA" }));
    }

    // --- name ---

    /// <summary>
    /// La ricerca per nome restituisce una voce per ente, con institutionId: gli omonimi restano distinti.
    /// </summary>
    [Test]
    public async Task Name_ShouldRestituireUnaVocePerEnte()
    {
        var resp = await Post($"{Base}/name", """{ "name": "Alfa" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(Valori(json, "institutionId"), Is.EqualTo(new[] { "APPIO-ENTE-1", "APPIO-ENTE-4" }));
            Assert.That(Valori(json, "name"), Is.All.EqualTo("Comune Alfa AppIO Test"));
        });
    }

    /// <summary>
    /// Un nome che non corrisponde a nessun ente risponde 404.
    /// </summary>
    [Test]
    public async Task Name_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/name", """{ "name": "Inesistente" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- griglia ---

    /// <summary>
    /// Senza filtri la griglia restituisce tutti i contratti con tutte le colonne, ordinati per nome.
    /// </summary>
    [Test]
    public async Task Grid_SenzaFiltri_ShouldRestituireTuttiIContratti()
    {
        var resp = await Post(Base, "{}", Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        var contratti = json.GetProperty("contratti");
        var ob1 = contratti.EnumerateArray().Single(x => x.GetProperty("contractId").GetString() == "APPIO-OB1");
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(Valori(contratti, "contractId"),
                Is.EqualTo(new[] { "APPIO-OB5", "APPIO-OB1", "APPIO-OB4", "APPIO-OB2", "APPIO-OB3" }));
            Assert.That(ob1.GetProperty("institutionId").GetString(), Is.EqualTo("APPIO-ENTE-1"));
            Assert.That(ob1.GetProperty("prodotto").GetString(), Is.EqualTo("prod-io"));
            Assert.That(ob1.GetProperty("institutionType").GetString(), Is.EqualTo("PA"));
            Assert.That(ob1.GetProperty("emailPec").GetString(), Is.EqualTo("alfa@pec.example.test"));
            Assert.That(ob1.GetProperty("contractState").GetString(), Is.EqualTo("ACTIVE"));
        });
    }

    /// <summary>
    /// Il filtro per ente usa gli institutionId del filtro "Nome Ente".
    /// </summary>
    [Test]
    public async Task Grid_PerEnte_ShouldRestituireIContrattiDellEnte()
    {
        var resp = await Post(Base, """{ "institutionIds": ["APPIO-ENTE-4"] }""", Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori((await Json(resp)).GetProperty("contratti"), "contractId"), Is.EqualTo(new[] { "APPIO-OB4" }));
    }

    /// <summary>
    /// Il filtro per tipo ente restituisce solo i contratti di quel tipo.
    /// </summary>
    [Test]
    public async Task Grid_PerTipoEnte_ShouldRestituireIContrattiDelTipo()
    {
        var resp = await Post(Base, """{ "tipiEnte": ["PA"] }""", Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Valori((await Json(resp)).GetProperty("contratti"), "contractId"),
            Is.EqualTo(new[] { "APPIO-OB1", "APPIO-OB2", "APPIO-OB3" }));
    }

    /// <summary>
    /// Ente e tipo ente si applicano insieme: un ente PA filtrato per GSP non trova nulla, 404.
    /// </summary>
    [Test]
    public async Task Grid_EnteETipoIncompatibili_ShouldReturn404()
    {
        var resp = await Post(Base, """{ "institutionIds": ["APPIO-ENTE-1"], "tipiEnte": ["GSP"] }""", Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// La paginazione restituisce la pagina richiesta e il count totale.
    /// </summary>
    [Test]
    public async Task Grid_Paginata_ShouldRestituireLaPaginaEIlCountTotale()
    {
        var resp = await Post(Base, "{}", "&page=2&pageSize=2");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Json(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(Valori(json.GetProperty("contratti"), "contractId"), Is.EqualTo(new[] { "APPIO-OB4", "APPIO-OB2" }));
        });
    }

    /// <summary>
    /// Senza autenticazione la griglia risponde 401.
    /// </summary>
    [Test]
    public async Task Grid_SenzaAutenticazione_ShouldReturn401()
    {
        var resp = await Post(Base, "{}", Paginazione, ruolo: null);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // --- download ---

    /// <summary>
    /// Il download restituisce un Excel con i contratti del filtro, senza paginazione.
    /// </summary>
    [Test]
    public async Task Download_ShouldRestituireUnExcelConIContrattiDelFiltro()
    {
        var resp = await Post($"{Base}/download", """{ "tipiEnte": ["GSP"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/vnd.ms-excel"));
        var testo = await TestoExcel(resp);
        Assert.Multiple(() =>
        {
            Assert.That(testo, Does.Contain("APPIO-OB4"));
            Assert.That(testo, Does.Contain("alfa.gsp@pec.example.test"));
            Assert.That(testo, Does.Not.Contain("APPIO-OB1"), "OB1 è PA");
        });
    }

    /// <summary>
    /// Un filtro che non trova contratti risponde 404 e non produce alcun file.
    /// </summary>
    [Test]
    public async Task Download_NessunRisultato_ShouldReturn404()
    {
        var resp = await Post($"{Base}/download", """{ "institutionIds": ["INESISTENTE"] }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
