using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili sulle rotte api/appio/contracts* (PF-908), cioè ciò che può mandare chi chiama l'API
/// direttamente invece di passare dal portale.
///
/// I test si dividono in due famiglie:
///  - <b>difese verificate</b>: l'injection resta un valore, il nonce di un altro prodotto è
///    rifiutato;
///  - <b>caratterizzazioni</b> (suffisso <c>_Caratterizzazione</c>): fissano il comportamento ATTUALE
///    dove è discutibile — jolly del LIKE non neutralizzati, 500 su paginazione fuori range, su liste
///    oltre il limite dei parametri di SQL Server e su body malformati. Non sono aspettative di
///    prodotto: se un domani il comportamento viene corretto diventano rossi, ed è il segnale per
///    aggiornarli, non per ripristinare il vecchio comportamento.
///
/// Seed (tests/Data/appio.sql): trimestre più recente 2026_2 con C1, C2, C3.
/// </summary>
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

    /// <summary>Serializza un oggetto anonimo come body JSON (con l'escape corretto degli apici).</summary>
    private static string Json(object o) => JsonSerializer.Serialize(o);

    /// <summary>Elenco dei contractId restituiti dalla ricerca per nome.</summary>
    private static async Task<List<string>> ContractIds(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Select(x => x.GetProperty("contractId").GetString()!).ToList();

    // --- injection: deve restare un valore ---

    /// <summary>
    /// Un tentativo di injection nel nome viene trattato come testo da cercare: nessun contratto
    /// corrisponde, quindi 404 — non un 500 e soprattutto non tutte le righe.
    /// </summary>
    [TestCase("' OR 1=1 --")]
    [TestCase("'; DROP TABLE appio.Contracts; --")]
    public async Task Name_Injection_ShouldRestareUnValore_404(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Dopo il tentativo di DROP la tabella esiste ancora con tutte le righe del seed.</summary>
    [Test]
    public async Task Name_InjectionDrop_ShouldLasciareIntattaLaTabella()
    {
        await Post($"{Base}/name", Json(new { name = "'; DROP TABLE appio.Contracts; --" }));

        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM appio.Contracts WHERE contract_id LIKE 'APPIO-C%'", conn);
        Assert.That((int)(await cmd.ExecuteScalarAsync())!, Is.EqualTo(6));
    }

    /// <summary>Injection nell'anno dei trimestri: nessun trimestre corrisponde, 404.</summary>
    [Test]
    public async Task Quarters_InjectionNellAnno_ShouldRestareUnValore_404()
    {
        var resp = await Post($"{Base}/quarters", Json(new { year = "2026' --" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Injection nei trimestri della griglia: finisce in un IN parametrizzato, 404.</summary>
    [Test]
    public async Task Grid_InjectionNeiTrimestri_ShouldRestareUnValore_404()
    {
        var resp = await Post(Base, """{ "quarters": ["' OR 1=1 --"] }""", Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- jolly del LIKE: non neutralizzati ---

    /// <summary>
    /// Il nome arriva nel LIKE senza escape, quindi '%', '_' e le classi '[a-z]' agiscono da jolly e
    /// restituiscono tutti i contratti del trimestre. Nessun rischio di injection (il valore resta un
    /// parametro), ma la ricerca non è letterale. Stesso comportamento delle rotte PSP.
    /// </summary>
    [TestCase("%")]
    [TestCase("_")]
    [TestCase("[a-z]")]
    public async Task Name_JollyDelLike_ShouldRestituireTuttiIContratti_Caratterizzazione(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await ContractIds(resp), Is.EquivalentTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3" }));
    }

    /// <summary>Una parentesi quadra non chiusa non solleva errore: semplicemente non trova nulla.</summary>
    [Test]
    public async Task Name_ParentesiNonChiusa_ShouldReturn404_Caratterizzazione()
    {
        var resp = await Post($"{Base}/name", Json(new { name = "[" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Anche l'anno dei trimestri finisce in un LIKE: '%' restituisce tutti i trimestri, come se
    /// l'anno non fosse stato indicato.
    /// </summary>
    [TestCase("%")]
    [TestCase("")]
    public async Task Quarters_AnnoJollyOVuoto_ShouldRestituireTuttiITrimestri_Caratterizzazione(string year)
    {
        var resp = await Post($"{Base}/quarters", Json(new { year }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var valori = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Select(x => x.GetProperty("value").GetString()).ToList();
        Assert.That(valori, Is.EqualTo(new[] { "2025_4", "2026_1", "2026_2" }));
    }

    // --- nome lunghissimo ---

    /// <summary>Un nome di 4000 caratteri viene cercato normalmente: nessuna corrispondenza, 404.</summary>
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
    /// producono un 500 (errore di binding appiattito dal gestore globale, oppure OFFSET/FETCH
    /// negativo in SQL), non un 400. Stesso comportamento delle rotte PSP.
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
    /// quando il filtro non trova nulla, non quando la pagina è vuota.
    /// </summary>
    [Test]
    public async Task Grid_PaginaOltreLaFine_ShouldReturn200ConListaVuotaECountTotale_Caratterizzazione()
    {
        var resp = await Post(Base, "{}", "&page=999&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(3));
            Assert.That(json.GetProperty("contratti").GetArrayLength(), Is.Zero);
        });
    }

    // --- liste lunghe ---

    /// <summary>2000 contractIds rientrano nel limite dei parametri: nessuna corrispondenza, 404.</summary>
    [Test]
    public async Task Grid_2000ContractIds_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { contractIds = Ids(2000) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Oltre circa 2100 contractIds si supera il limite dei parametri di SQL Server (Dapper espande
    /// l'IN in un parametro per elemento) e la rotta risponde 500. Stesso limite già visto su
    /// Gestione Fatture.
    /// </summary>
    [TestCase(2100)]
    [TestCase(3000)]
    public async Task Grid_OltreIlLimiteDeiParametri_ShouldReturn500_Caratterizzazione(int n)
    {
        var resp = await Post(Base, Json(new { contractIds = Ids(n) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>n contractId fittizi, inesistenti nel seed.</summary>
    private static string[] Ids(int n) => Enumerable.Range(0, n).Select(i => $"X{i}").ToArray();

    // --- body ---

    /// <summary>
    /// Body malformato, vuoto, letterale null o con un tipo sbagliato (stringa al posto di array)
    /// producono un 500 invece di un 400: l'errore di binding viene appiattito dal gestore globale,
    /// e un body null arriva fino al mapping.
    /// </summary>
    [TestCase("{ malformato")]
    [TestCase("")]
    [TestCase("null")]
    [TestCase("""{ "quarters": "2026_2" }""")]
    public async Task Grid_BodyNonValido_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(Base, body, Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    // --- sessione ---

    /// <summary>
    /// Un nonce emesso per un altro prodotto (qui prod-appio contro un token prod-pn) viene rifiutato
    /// con 419 dal NonceMultiTabsMiddleware, come sulle altre rotte admin.
    /// </summary>
    [Test]
    public async Task Grid_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post(Base, "{}", Paginazione, prodottoNonce: "prod-appio");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
    }
}
