using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Auth.PagoPA;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Casi ostili sul claim <c>Prodotto</c> del token admin (PF-908, arrivo di APP IO), con token REALI
/// (JwtApiTestFactory). Il login admin emette un token per prodotto (pagoPA, SEND, APP IO) che
/// differiscono SOLO per quel claim; nessuna policy lo guarda (PagoPAPolicy richiede solo
/// auth = PAGOPA), quindi l'unico punto del backend che distingue i prodotti e' il nonce.
///
/// Tre famiglie, parametrizzate su tutti e tre i prodotti:
/// 1. nonce di un prodotto con il token di un altro -> 419. E' il multi-tab spostato dal cambio di
///    ente (coperto da NonceMultiTabsHttpTests) al cambio di prodotto: due tab dello stesso operatore,
///    una su pagoPA e una su APP IO.
/// 2. CARATTERIZZAZIONE: prodotto sconosciuto -> la richiesta passa. Non c'e' whitelist dei prodotti;
///    diventera' rosso il giorno in cui arriva una policy per prodotto, ed e' il segnale per aggiornarlo.
/// 3. CARATTERIZZAZIONE: claim Prodotto assente -> 500, non 401. IdentityExtensions.Mapper fa
///    FirstOrDefault(...)!.Value su ogni claim: un token firmato ma incompleto diventa una
///    NullReferenceException. Difetto preesistente, qui solo fissato.
///
/// I rifiuti (419, 500) non toccano il DB: decidono prima dell'endpoint. I casi che superano auth e
/// nonce arrivano invece al DB seedato e asseriscono 200 con dati sul periodo 2026/2.
/// </summary>
public class ProdottoTokenAdversarialHttpTests
{
    private const int SessionExpired = 419;
    private const string RottaAdmin = "/api/fatture";                      // PagoPAPolicy, fuori whitelist nonce
    private const string RottaProfilo = "/" + JwtApiTestFactory.RottaProtetta; // in whitelist nonce
    // 2026/2: periodo con fatture emesse nel seed (8001 e seguenti, ente1), quindi i casi che superano
    // auth e nonce possono asserire un 200 con dati invece di un generico "non rifiutato".
    private const string Body = """{ "cancellata": false, "anno": 2026, "mese": 2 }""";

    private static readonly string[] Prodotti = [ProductRoles.pagoPA, ProductRoles.SEND, ProductRoles.AppIO];

    private JwtApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new JwtApiTestFactory();

    [SetUp]
    public void CheckConfigurazione() => _factory.SkipSeConfigurazioneJwtAssente();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private string TokenAdmin(string? prodotto) =>
        _factory.Token(ruolo: Ruolo.ADMIN, auth: AuthType.PAGOPA, profilo: Profilo.Approvigionamento, prodotto: prodotto);

    private async Task<HttpResponseMessage> PostAdmin(string? prodottoToken, string prodottoNonce)
    {
        var resp = await _factory.CreateClientWithToken(TokenAdmin(prodottoToken)).PostAsync(
            _factory.WithNonce(RottaAdmin, prodotto: prodottoNonce),
            new StringContent(Body, Encoding.UTF8, "application/json"));
        TestContext.Out.WriteLine($"token={prodottoToken ?? "(nessun claim)"} nonce={prodottoNonce} -> {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    // Niente SetName: un nome personalizzato sostituisce il nome completo del caso, che non inizia piu'
    // con quello del metodo — e Test Explorer, avviando il metodo, non trova nessun caso da eseguire.
    private static IEnumerable<TestCaseData> CoppieDiverse() =>
        from t in Prodotti
        from n in Prodotti
        where t != n
        select new TestCaseData(t, n);

    [TestCaseSource(nameof(CoppieDiverse))]
    public async Task NonceDiAltroProdotto_ShouldReturn419(string prodottoToken, string prodottoNonce)
    {
        var resp = await PostAdmin(prodottoToken, prodottoNonce);

        Assert.That((int)resp.StatusCode, Is.EqualTo(SessionExpired),
            "Il nonce emesso per un prodotto non deve valere con il token di un altro: e' la sola barriera fra prodotti.");
    }

    /// <summary>
    /// Controllo positivo: prova che il 419 sopra viene dal prodotto e non da altro nella pipeline, e che
    /// con il token di qualunque prodotto la rotta admin restituisce davvero i dati del periodo.
    /// </summary>
    [TestCase(ProductRoles.pagoPA)]
    [TestCase(ProductRoles.SEND)]
    [TestCase(ProductRoles.AppIO)]
    public async Task NonceDelloStessoProdotto_ShouldReturn200_ConDati(string prodotto)
    {
        var resp = await PostAdmin(prodotto, prodotto);

        await AssertOkConDati(resp);
    }

    private static async Task AssertOkConDati(HttpResponseMessage resp)
    {
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        using var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array));
        Assert.That(json.RootElement.GetArrayLength(), Is.GreaterThan(0), "il seed ha fatture emesse su 2026/2");
    }

    /// <summary>
    /// CARATTERIZZAZIONE, non approvazione: un prodotto che il login non emette mai viene accettato,
    /// perche' nessuna policy verifica il claim. Richiede comunque un token firmato con il nostro secret,
    /// quindi non e' sfruttabile dall'esterno; e' la documentazione della scelta del 30/09/2026 di
    /// rimandare la policy per prodotto. Quando arrivera', questo test va riscritto (atteso 403).
    /// </summary>
    [Test]
    public async Task ProdottoSconosciuto_ShouldEssereAccettato_Caratterizzazione()
    {
        var resp = await PostAdmin("prod-inesistente", "prod-inesistente");

        // Comportamento attuale: nessuna whitelist dei prodotti, e i dati escono comunque. V. summary.
        await AssertOkConDati(resp);
    }

    /// <summary>
    /// CARATTERIZZAZIONE: token valido ma senza claim Prodotto -> 500 sulla rotta protetta.
    /// Il middleware del nonce chiama GetAuthInfo prima di tutto, e il Mapper dei claim dereferenzia
    /// un claim assente. L'esito desiderabile sarebbe 401 (token non conforme); se si corregge il
    /// Mapper, questo test va aggiornato, non cancellato.
    /// </summary>
    [Test]
    public async Task ClaimProdottoAssente_SuRottaProtetta_ShouldReturn500_Caratterizzazione()
    {
        var resp = await PostAdmin(null, ProductRoles.pagoPA);

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }

    /// <summary>
    /// Stesso difetto sulla rotta del profilo, che e' in whitelist del nonce: qui a dereferenziare il
    /// claim assente e' l'handler (context.GetAuthInfo in ProfiloAsync). Serve a mostrare che il 500
    /// non dipende dal middleware del nonce ma dal Mapper dei claim.
    /// </summary>
    [Test]
    public async Task ClaimProdottoAssente_SuProfilo_ShouldReturn500_Caratterizzazione()
    {
        var resp = await _factory.CreateClientWithToken(TokenAdmin(null)).GetAsync(RottaProfilo);
        TestContext.Out.WriteLine($"{RottaProfilo} -> {(int)resp.StatusCode} {resp.StatusCode}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
    }
}
