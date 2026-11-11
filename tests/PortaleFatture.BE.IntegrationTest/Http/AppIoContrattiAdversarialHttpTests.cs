using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili sulle rotte api/appio/contracts* (modello dati dell'11/11/2026, PF-1016), cioè ciò
/// che può mandare chi chiama l'API direttamente invece di passare dal portale.
///
/// I test si dividono in due famiglie:
///  - <b>difese verificate</b>: l'injection resta un valore, il nonce di un altro prodotto è
///    rifiutato;
///  - <b>caratterizzazioni</b> (suffisso <c>_Caratterizzazione</c>): fissano il comportamento ATTUALE
///    dove è discutibile — jolly del LIKE non neutralizzati, 500 su paginazione fuori range, su liste
///    oltre il limite dei parametri di SQL Server e su body malformati. Se un domani il comportamento
///    viene corretto diventano rossi, ed è il segnale per aggiornarli.
///
/// Seed (tests/Data/appio.sql): 5 contratti, 4 enti con nome.
/// </summary>
[Category("AppIO")]
public class AppIoContrattiAdversarialHttpTests
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
    }

    /// <summary>POST con utente ADMIN e nonce valido (salvo un prodotto diverso nel nonce).</summary>
    private async Task<HttpResponseMessage> Post(string rotta, string body, string query = "", string prodottoNonce = "prod-pn")
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(_factory.WithNonce(rotta, prodotto: prodottoNonce) + query, content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    private static string Json(object o) => JsonSerializer.Serialize(o);

    private static async Task<List<string>> InstitutionIds(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Select(x => x.GetProperty("institutionId").GetString()!).ToList();

    // --- injection: deve restare un valore ---

    /// <summary>
    /// Un tentativo di injection nel nome viene trattato come testo da cercare: nessun ente
    /// corrisponde, quindi 404 — non un 500 e soprattutto non tutte le righe.
    /// </summary>
    [TestCase("' OR 1=1 --")]
    [TestCase("'; DROP TABLE appio.contracts; --")]
    public async Task Name_Injection_ShouldRestareUnValore_404(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Dopo il tentativo di DROP la tabella esiste ancora con tutte le righe del seed.</summary>
    [Test]
    public async Task Name_InjectionDrop_ShouldLasciareIntattaLaTabella()
    {
        await Post($"{Base}/name", Json(new { name = "'; DROP TABLE appio.contracts; --" }));

        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM appio.contracts WHERE onboardingtokenid LIKE 'APPIO-OB%'", conn);
        Assert.That((int)(await cmd.ExecuteScalarAsync())!, Is.EqualTo(5));
    }

    /// <summary>Injection negli enti e nei tipi ente della griglia: finisce in un IN parametrizzato, 404.</summary>
    [Test]
    public async Task Grid_InjectionNeiFiltri_ShouldRestareUnValore_404()
    {
        var enti = await Post(Base, """{ "institutionIds": ["' OR 1=1 --"] }""", Paginazione);
        var tipi = await Post(Base, """{ "tipiEnte": ["' OR 1=1 --"] }""", Paginazione);

        Assert.Multiple(() =>
        {
            Assert.That(enti.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(tipi.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    // --- jolly del LIKE: non neutralizzati ---

    /// <summary>
    /// Il nome finisce in un LIKE senza escape: '%', '_' e '[a-z]' trovano tutti gli enti invece di
    /// cercare il carattere letterale. Stesso comportamento della ricerca PSP.
    /// </summary>
    [TestCase("%")]
    [TestCase("_")]
    [TestCase("[a-z]")]
    public async Task Name_JollyDelLike_ShouldRestituireTuttiGliEnti_Caratterizzazione(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await InstitutionIds(resp), Has.Count.EqualTo(4));
    }

    /// <summary>
    /// Una parentesi quadra non chiusa non è un pattern valido per il LIKE: non trova nulla, 404.
    /// </summary>
    [Test]
    public async Task Name_ParentesiNonChiusa_ShouldReturn404_Caratterizzazione()
    {
        var resp = await Post($"{Base}/name", Json(new { name = "[" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Un nome di 4000 caratteri non trova nulla: 404.</summary>
    [Test]
    public async Task Name_4000Caratteri_ShouldReturn404()
    {
        var resp = await Post($"{Base}/name", Json(new { name = new string('a', 4000) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Un nome di 100.000 caratteri fa fallire la query (il pattern del LIKE ha un limite di
    /// lunghezza in SQL Server) e la rotta risponde 500 invece di un 400 con messaggio.
    /// </summary>
    [Test]
    public async Task Name_100000Caratteri_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post($"{Base}/name", Json(new { name = new string('a', 100_000) }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    // --- paginazione ---

    /// <summary>
    /// page e pageSize sono obbligatori e non validati: assenti, non numerici, a zero o negativi
    /// producono un 500, non un 400. Stesso comportamento delle rotte PSP.
    /// </summary>
    [TestCase("")]
    [TestCase("&page=abc&pageSize=10")]
    [TestCase("&page=0&pageSize=10")]
    [TestCase("&page=-1&pageSize=10")]
    [TestCase("&page=1&pageSize=0")]
    [TestCase("&page=1&pageSize=-5")]
    public async Task Grid_PaginazioneNonValida_ShouldReturn500_Caratterizzazione(string query)
    {
        var resp = await Post(Base, "{}", query);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Una pagina oltre la fine risponde 200 con lista vuota e il count totale: il 404 scatta solo
    /// quando il filtro non trova nulla.
    /// </summary>
    [Test]
    public async Task Grid_PaginaOltreLaFine_ShouldReturn200ConListaVuotaECountTotale_Caratterizzazione()
    {
        var resp = await Post(Base, "{}", "&page=999&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(json.GetProperty("contratti").GetArrayLength(), Is.Zero);
        });
    }

    // --- liste lunghe ---

    /// <summary>2000 institutionIds rientrano nel limite dei parametri: nessuna corrispondenza, 404.</summary>
    [Test]
    public async Task Grid_2000InstitutionIds_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { institutionIds = Ids(2000) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Oltre circa 2100 institutionIds si supera il limite dei parametri di SQL Server (Dapper espande
    /// l'IN in un parametro per elemento) e la rotta risponde 500.
    /// </summary>
    [TestCase(2100)]
    [TestCase(3000)]
    public async Task Grid_OltreIlLimiteDeiParametri_ShouldReturn500_Caratterizzazione(int n)
    {
        var resp = await Post(Base, Json(new { institutionIds = Ids(n) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    private static string[] Ids(int n) => Enumerable.Range(0, n).Select(i => $"X{i}").ToArray();

    // --- body ---

    /// <summary>
    /// Body malformato o con un tipo sbagliato (stringa al posto di array) producono un 500 invece
    /// di un 400: l'errore di binding viene appiattito dal gestore globale.
    /// </summary>
    [TestCase("{ malformato")]
    [TestCase("""{ "tipiEnte": "PA" }""")]
    public async Task Grid_BodyNonValido_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(Base, body, Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Tutti i filtri sono opzionali, body compreso: vuoto, null o "{}" significano nessun filtro,
    /// su griglia e download.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public async Task GridEDownload_BodyVuotoONull_ShouldRestituireTuttiIContratti(string body)
    {
        var grid = await Post(Base, body, Paginazione);
        var download = await Post($"{Base}/download", body);

        Assert.That(grid.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = (await JsonDocument.ParseAsync(await grid.Content.ReadAsStreamAsync())).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Anche sulla ricerca per nome il body è opzionale: vuoto, null o "{}" danno tutti gli enti con nome.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public async Task Name_BodyVuotoONull_ShouldRestituireTuttiGliEnti(string body)
    {
        var resp = await Post($"{Base}/name", body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await InstitutionIds(resp),
            Is.EquivalentTo(new[] { "APPIO-ENTE-1", "APPIO-ENTE-2", "APPIO-ENTE-3", "APPIO-ENTE-4" }));
    }

    /// <summary>
    /// Anche una POST senza alcun contenuto (nessun body, nessun Content-Type) vale come "{}".
    /// </summary>
    [Test]
    public async Task GridEDownload_SenzaContenuto_ShouldRestituireTuttiIContratti()
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var grid = await client.PostAsync(_factory.WithNonce(Base) + Paginazione, null);
        var download = await client.PostAsync(_factory.WithNonce($"{Base}/download"), null);

        Assert.Multiple(() =>
        {
            Assert.That(grid.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    // --- sessione ---

    /// <summary>
    /// Un nonce emesso per un altro prodotto viene rifiutato con 419 dal NonceMultiTabsMiddleware,
    /// come sulle altre rotte admin.
    /// </summary>
    [Test]
    public async Task Grid_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post(Base, "{}", Paginazione, prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
    }
}
