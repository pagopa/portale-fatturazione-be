using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Input ostili sulle rotte api/v2/pagopa/psps* (anagrafica PSP): ciò che può mandare chi chiama
/// l'API direttamente invece di passare dal portale.
///
/// Due famiglie:
///  - <b>difese verificate</b>: l'injection resta un valore, il nonce di un altro prodotto è
///    rifiutato;
///  - <b>caratterizzazioni</b> (suffisso <c>_Caratterizzazione</c>): fissano il comportamento ATTUALE
///    dove è discutibile. Non sono aspettative di prodotto: se il comportamento viene corretto
///    diventano rossi, ed è il segnale per aggiornarli.
///
/// Seed (tests/Data/ppa_contracts.sql): trimestre più recente 2026_1 con T01…T05; T01 'ABI01234' e
/// T03 'ABI01235,BICGAMMA' condividono il prefisso 0123.
/// </summary>
public class PSPAdversarialHttpTests
{
    private const string Base = "/api/v2/pagopa/psps";
    private const string Paginazione = "&page=1&pageSize=10";

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

    /// <summary>Corpo della risposta come JSON.</summary>
    private static async Task<JsonElement> Leggi(HttpResponseMessage resp)
        => JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;

    /// <summary>Elenco dei contractId della griglia.</summary>
    private static async Task<List<string>> IdGriglia(HttpResponseMessage resp)
        => (await Leggi(resp)).GetProperty("psPs").EnumerateArray()
            .Select(x => x.GetProperty("contractId").GetString()!).ToList();

    /// <summary>n contractId fittizi, inesistenti nel seed.</summary>
    private static string[] Ids(int n) => Enumerable.Range(0, n).Select(i => $"X{i}").ToArray();

    // --- injection: deve restare un valore ---

    /// <summary>
    /// Injection nei filtri testuali della griglia (ABI, membership, recipient): finiscono in
    /// parametri, nessuna riga corrisponde, 404.
    /// </summary>
    [TestCase("abi")]
    [TestCase("membershipId")]
    [TestCase("recipientId")]
    public async Task Grid_InjectionNeiFiltri_ShouldRestareUnValore_404(string campo)
    {
        var body = $$"""{ "{{campo}}": "' OR 1=1 --" }""";

        var resp = await Post(Base, body, Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Injection nel nome: trattata come testo da cercare, 404.</summary>
    [Test]
    public async Task Name_Injection_ShouldRestareUnValore_404()
    {
        var resp = await Post($"{Base}/name", Json(new { name = "'; DROP TABLE ppa.Contracts; --" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Dopo il tentativo di DROP la tabella esiste ancora con tutte le righe del seed.</summary>
    [Test]
    public async Task Name_InjectionDrop_ShouldLasciareIntattaLaTabella()
    {
        await Post($"{Base}/name", Json(new { name = "'; DROP TABLE ppa.Contracts; --" }));

        await using var conn = new SqlConnection(LocalTestDb.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM ppa.Contracts WHERE contract_id LIKE 'PSP-T%'", conn);
        Assert.That((int)(await cmd.ExecuteScalarAsync())!, Is.EqualTo(7));
    }

    /// <summary>Injection nell'anno dei trimestri: nessun trimestre corrisponde, 404.</summary>
    [Test]
    public async Task Quarters_InjectionNellAnno_ShouldRestareUnValore_404()
    {
        var resp = await Post($"{Base}/quarters", Json(new { year = "2026' --" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    // --- filtro ABI ---

    /// <summary>
    /// Il ramo <c>provider_names LIKE '%ABI' + @abi + '%'</c> è una ricerca per PREFISSO: un ABI
    /// parziale trova tutti i codici che iniziano così. Cercando '0123' escono sia T01 ('ABI01234')
    /// sia T03 ('ABI01235'). Il filtro ABI va quindi usato con il codice completo a 5 cifre.
    /// </summary>
    [Test]
    public async Task Grid_AbiParziale_ShouldTrovarePiuPspPerPrefisso_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { abi = "0123" }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await IdGriglia(resp), Is.EqualTo(new[] { "PSP-T01", "PSP-T03" }));
    }

    /// <summary>
    /// '%' come ABI agisce da jolly in tre rami del LIKE e restituisce tutti i PSP che hanno
    /// provider_names valorizzato (T01, T02, T03); restano fuori quello con provider NULL e quello
    /// con provider vuoto.
    /// </summary>
    [Test]
    public async Task Grid_AbiJolly_ShouldTrovareTuttiQuelliConProvider_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { abi = "%" }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await IdGriglia(resp), Is.EqualTo(new[] { "PSP-T01", "PSP-T02", "PSP-T03" }));
    }

    /// <summary>Un ABI vuoto è trattato come filtro assente: tutto il trimestre più recente.</summary>
    [Test]
    public async Task Grid_AbiVuoto_ShouldEssereIgnorato()
    {
        var resp = await Post(Base, Json(new { abi = "" }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await IdGriglia(resp), Has.Count.EqualTo(5));
    }

    // --- ricerca per nome ---

    /// <summary>
    /// Senza nome (campo assente o null) la condizione diventa <c>name LIKE '%' + NULL + '%'</c>, che
    /// in SQL non è mai vera: risponde 404 invece di restituire tutti i contratti. È diverso dalle
    /// rotte APP IO, dove un nome assente vale "nessun filtro".
    /// </summary>
    [TestCase("{}")]
    [TestCase("""{ "name": null }""")]
    public async Task Name_Assente_ShouldReturn404_Caratterizzazione(string body)
    {
        var resp = await Post($"{Base}/name", body);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// I caratteri jolly del LIKE nel nome non sono neutralizzati: '%' e '_' restituiscono tutti i
    /// contratti del trimestre più recente.
    /// </summary>
    [TestCase("%")]
    [TestCase("_")]
    public async Task Name_JollyDelLike_ShouldRestituireTuttiIContratti_Caratterizzazione(string name)
    {
        var resp = await Post($"{Base}/name", Json(new { name }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Leggi(resp)).GetArrayLength(), Is.EqualTo(5));
    }

    // --- trimestri ---

    /// <summary>
    /// Senza anno l'etichetta del trimestre si rompe: il modulo calcola
    /// <c>"Q" + value.Replace(year + "_", "")</c>, e con year null toglie solo il trattino basso,
    /// producendo "Q20261" invece di "Q1". La rotta years non ne risente, perché usa solo il valore.
    /// </summary>
    [Test]
    public async Task Quarters_SenzaAnno_ShouldProdurreEtichetteSbagliate_Caratterizzazione()
    {
        var resp = await Post($"{Base}/quarters", "{}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var quarters = (await Leggi(resp)).EnumerateArray().Select(x => x.GetProperty("quarter").GetString()).ToList();
        Assert.That(quarters, Is.EqualTo(new[] { "Q20254", "Q20261" }));
    }

    /// <summary>
    /// L'anno è cercato come sottostringa (<c>LIKE '%' + @year + '%'</c>), non come prefisso: '1'
    /// trova 2026_1 e l'etichetta resta intera, perché "1_" non compare nel valore.
    /// </summary>
    [Test]
    public async Task Quarters_AnnoParziale_ShouldTrovarePerSottostringa_Caratterizzazione()
    {
        var resp = await Post($"{Base}/quarters", Json(new { year = "1" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Leggi(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.EnumerateArray().Select(x => x.GetProperty("value").GetString()), Is.EqualTo(new[] { "2026_1" }));
            Assert.That(json.EnumerateArray().Select(x => x.GetProperty("quarter").GetString()), Is.EqualTo(new[] { "Q2026_1" }));
        });
    }

    /// <summary>'%' come anno agisce da jolly e restituisce tutti i trimestri.</summary>
    [Test]
    public async Task Quarters_AnnoJolly_ShouldRestituireTuttiITrimestri_Caratterizzazione()
    {
        var resp = await Post($"{Base}/quarters", Json(new { year = "%" }));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await Leggi(resp)).GetArrayLength(), Is.EqualTo(2));
    }

    // --- paginazione ---

    /// <summary>
    /// page e pageSize sono obbligatori e non validati: assenti, non numerici, a zero o negativi
    /// producono un 500, non un 400.
    /// </summary>
    [TestCase("")]
    [TestCase("&page=abc&pageSize=10")]
    [TestCase("&page=0&pageSize=10")]
    [TestCase("&page=-1&pageSize=10")]
    [TestCase("&page=1&pageSize=0")]
    public async Task Grid_PaginazioneNonValida_ShouldReturn500_Caratterizzazione(string query)
    {
        var resp = await Post(Base, "{}", query);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Una pagina oltre la fine risponde 200 con lista vuota e conteggio totale: il 404 scatta solo
    /// quando il filtro non trova nulla.
    /// </summary>
    [Test]
    public async Task Grid_PaginaOltreLaFine_ShouldReturn200ConListaVuota_Caratterizzazione()
    {
        var resp = await Post(Base, "{}", "&page=999&pageSize=10");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var json = await Leggi(resp);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("count").GetInt32(), Is.EqualTo(5));
            Assert.That(json.GetProperty("psPs").GetArrayLength(), Is.Zero);
        });
    }

    // --- liste lunghe e body ---

    /// <summary>Oltre ~2100 contractIds si supera il limite dei parametri di SQL Server: 500.</summary>
    [Test]
    public async Task Grid_OltreIlLimiteDeiParametri_ShouldReturn500_Caratterizzazione()
    {
        var resp = await Post(Base, Json(new { contractIds = Ids(3000) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>2000 contractIds rientrano nel limite: nessuna corrispondenza, 404.</summary>
    [Test]
    public async Task Grid_2000ContractIds_ShouldReturn404()
    {
        var resp = await Post(Base, Json(new { contractIds = Ids(2000) }), Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Body malformato, vuoto o letterale null producono un 500 invece di un 400.
    /// </summary>
    [TestCase("{ malformato")]
    [TestCase("")]
    [TestCase("null")]
    public async Task Grid_BodyNonValido_ShouldReturn500_Caratterizzazione(string body)
    {
        var resp = await Post(Base, body, Paginazione);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    // --- sessione ---

    /// <summary>
    /// Un nonce emesso per un altro prodotto viene rifiutato con 419, come sulle altre rotte admin.
    /// </summary>
    [Test]
    public async Task Grid_NonceDiUnAltroProdotto_ShouldReturn419()
    {
        var resp = await Post(Base, "{}", Paginazione, prodottoNonce: "prod-pagopa");

        Assert.That((int)resp.StatusCode, Is.EqualTo(419));
    }
}
