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

    /// <summary>Injection nell'anno della griglia e della ricerca per nome: resta un valore, 404.</summary>
    [Test]
    public async Task GridENome_InjectionNellAnno_ShouldRestareUnValore_404()
    {
        var grid = await Post(Base, Json(new { year = "2026' OR 1=1 --" }), Paginazione);
        var nome = await Post($"{Base}/name", Json(new { name = "AppIO", year = "2026' OR 1=1 --" }));

        Assert.Multiple(() =>
        {
            Assert.That(grid.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(nome.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    // --- jolly del LIKE: non neutralizzati ---

    /// <summary>
    /// L'anno della griglia finisce in un LIKE senza escape: '%' prende tutti i trimestri (le 6 righe
    /// del seed) invece di ricadere sul più recente. Stesso comportamento dei documenti contabili APP IO.
    /// </summary>
    [Test]
    public async Task Grid_AnnoJolly_ShouldRestituireTuttiITrimestri_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { year = "%" }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("count").GetInt32(), Is.EqualTo(6));
    }

    /// <summary>
    /// Il trattino basso nell'anno è un jolly di un carattere: '20_6' prende il 2026 (4 righe). Il
    /// separatore 'AAAA_T' invece è protetto da '[_]', quindi non può fare da jolly.
    /// </summary>
    [Test]
    public async Task Grid_AnnoConTrattinoBasso_ShouldFareDaJolly_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { year = "20_6" }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("count").GetInt32(), Is.EqualTo(4));
    }

    /// <summary>
    /// Un trimestre passato nel campo dell'anno ('2026_1') non corrisponde a nulla: 404 su griglia e
    /// nome. Non viene interpretato come trimestre, né ricade sul default.
    /// </summary>
    [Test]
    public async Task GridENome_TrimestreAlPostoDellAnno_ShouldReturn404()
    {
        var grid = await Post(Base, Json(new { year = "2026_1" }), Paginazione);
        var nome = await Post($"{Base}/name", Json(new { name = "AppIO", year = "2026_1" }));

        Assert.Multiple(() =>
        {
            Assert.That(grid.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(nome.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    /// <summary>
    /// Anno vuoto o di soli spazi vale "nessun anno": sulla griglia ricade sul trimestre più recente
    /// (3 righe), sul nome su tutti i trimestri.
    /// </summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task Grid_AnnoVuoto_ShouldUsareIlTrimestrePiuRecente(string year)
    {
        var resp = await Post(Base, Json(new { year }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync()))
            .RootElement.GetProperty("count").GetInt32(), Is.EqualTo(3));
    }

    /// <summary>
    /// Un anno di 5000 caratteri fa fallire la query: oltre 4000 Dapper passa a nvarchar(max) e SQL
    /// Server rifiuta il pattern del LIKE. 500, non 400. Stesso limite dei documenti contabili APP IO.
    /// </summary>
    [Test]
    public async Task Grid_Anno5000Caratteri_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { year = new string('9', 5000) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Il nome arriva nel LIKE senza escape, quindi '%', '_' e le classi '[a-z]' agiscono da jolly e
    /// restituiscono tutti i contratti. Senza trimestri né anno si cerca in tutti i trimestri, e il
    /// GROUP BY dà una riga per contratto (i 4 del seed, che ha 6 righe). Nessun rischio di injection
    /// (il valore resta un parametro), ma la ricerca non è letterale. Stesso comportamento delle rotte PSP.
    /// </summary>
    [TestCase("%")]
    [TestCase("_")]
    [TestCase("[a-z]")]
    public async Task Name_JollyDelLike_ShouldRestituireTuttiIContratti_Caratterizzazione(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await ContractIds(resp), Is.EquivalentTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3", "APPIO-C4" }));
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
    /// Body malformato o con un tipo sbagliato (stringa al posto di array) producono un 500 invece
    /// di un 400: l'errore di binding viene appiattito dal gestore globale.
    /// </summary>
    [TestCase("{ malformato")]
    [TestCase("""{ "quarters": "2026_2" }""")]
    public async Task Grid_BodyNonValido_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(Base, body, Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Tutti i filtri sono opzionali, body compreso: body vuoto, letterale null o assente valgono
    /// come "{}", cioè il trimestre più recente (2026_2 nel seed), su griglia e download.
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public async Task GridEDownload_BodyVuotoONull_ShouldUsareIlTrimestrePiuRecente(string body)
    {
        var grid = await Post(Base, body, Paginazione);
        var download = await Post($"{Base}/download", body);

        Assert.That(grid.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = (await JsonDocument.ParseAsync(await grid.Content.ReadAsStreamAsync())).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(download.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(3));
            Assert.That(json.GetProperty("contratti").EnumerateArray().Select(x => x.GetProperty("yearQuarter").GetString()),
                Is.All.EqualTo("2026_2"));
        });
    }

    /// <summary>
    /// Anche sulla ricerca per nome il body è opzionale: vuoto, null o "{}" significano nessun filtro
    /// su nome e periodo, quindi tutti i contratti di tutti i trimestri, uno per id (i 4 del seed).
    /// </summary>
    [TestCase("")]
    [TestCase("null")]
    [TestCase("{}")]
    public async Task Name_BodyVuotoONull_ShouldRestituireTuttiIContratti(string body)
    {
        var resp = await Post($"{Base}/name", body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await ContractIds(resp), Is.EquivalentTo(new[] { "APPIO-C1", "APPIO-C2", "APPIO-C3", "APPIO-C4" }));
    }

    /// <summary>
    /// Anche una POST senza alcun contenuto (nessun body, nessun Content-Type) vale come "{}".
    /// </summary>
    [Test]
    public async Task GridEDownload_SenzaContenuto_ShouldUsareIlTrimestrePiuRecente()
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
