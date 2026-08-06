using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Test HTTP sul NonceMultiTabsMiddleware — il ramo di RIFIUTO, che fino ad ora non era coperto:
/// tutti gli altri test HTTP attraversano il middleware con un nonce valido (WithNonce), quindi
/// esercitano solo il caso felice. Qui si verifica che senza nonce, o con il nonce di un'altra
/// identita', la richiesta venga davvero respinta.
///
/// Copre i casi BE-AUTH-06 e BE-AUTH-07 di docs/testbook-backend.md.
///
/// Cosa protegge: due tab dello stesso browser autenticate su due Enti diversi. Il login piu' recente
/// invalida il nonce del tab precedente, che deve ricevere un logout forzato invece di continuare a
/// operare con l'identita' sbagliata (scenario tipico degli operatori di supporto SEND).
///
/// NON serve il container di test: il middleware gira PRIMA dell'endpoint, quindi i casi di rifiuto
/// non toccano il DB. I due controlli positivi asseriscono solo "non 419", perche' il loro esito
/// dipende dal seed e non e' quello che stiamo misurando.
/// </summary>
public class NonceMultiTabsHttpTests
{
    /// <summary>SessionException -> 419 (mappatura in ConfigurationExtensions.UseModules).</summary>
    private const int SessionExpired = 419;

    private const string RottaProtetta = "/api/fatture";
    private const string RottaInWhitelist = "/api/auth/profilo";

    /// <summary>IdEnte diverso da quello emesso da TestAuthHandler (11111111-...).</summary>
    private const string AltroEnte = "22222222-2222-2222-2222-222222222222";

    private ApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new ApiTestFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Post(string rotta)
    {
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent("""{ "anno": 2024, "mese": 2 }""", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(rotta, content);
        TestContext.Out.WriteLine($"{rotta} -> STATUS: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    [Test]
    public async Task NonceAssente_SuRottaProtetta_ShouldReturn419()
    {
        // Stessa richiesta dei test di FattureRicercaHttpTests, ma senza ?nonce= in query string.
        var resp = await Post(RottaProtetta);

        Assert.That((int)resp.StatusCode, Is.EqualTo(SessionExpired),
            "Senza nonce il middleware deve sollevare SessionException -> 419, non lasciar passare la richiesta.");
    }

    [Test]
    public async Task NonceDiAltraIdentita_SuRottaProtetta_ShouldReturn419()
    {
        // Il token e' dell'ente 1111...; il nonce e' cifrato sull'ente 2222... (l'altro tab).
        // Verify() confronta Id + IdEnte + Prodotto: il mismatch su IdEnte deve bastare a respingere.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent("""{ "anno": 2024, "mese": 2 }""", Encoding.UTF8, "application/json");

        var resp = await client.PostAsync(_factory.WithNonce(RottaProtetta, idEnte: AltroEnte), content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");

        Assert.That((int)resp.StatusCode, Is.EqualTo(SessionExpired),
            "Il nonce di un'altra identita' non deve essere accettato: e' esattamente il caso multi-tab.");
    }

    [Test]
    public async Task NonceValido_SuRottaProtetta_ShouldNotReturn419()
    {
        // Controllo positivo: serve a dimostrare che il 419 dei due test sopra viene dal nonce e non
        // da altro nella pipeline. Non si asserisce 200: l'esito dipende dal seed (e senza container
        // sarebbe 500), mentre qui interessa solo che il middleware lasci passare.
        var resp = await Post(_factory.WithNonce(RottaProtetta));

        Assert.That((int)resp.StatusCode, Is.Not.EqualTo(SessionExpired),
            "Con un nonce coerente con l'identity il middleware deve lasciar proseguire la richiesta.");
    }

    [Test]
    public async Task RottaInWhitelist_SenzaNonce_ShouldNotReturn419()
    {
        // api/auth/profilo e' in whitelist: e' l'unico modo che ha il client di ottenere un nonce,
        // quindi non puo' a sua volta pretenderlo. Se un domani sparisse dalla whitelist, il login
        // del portale si romperebbe: questo test lo intercetta.
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(RottaInWhitelist);
        TestContext.Out.WriteLine($"{RottaInWhitelist} -> STATUS: {(int)resp.StatusCode} {resp.StatusCode}");

        Assert.That((int)resp.StatusCode, Is.Not.EqualTo(SessionExpired),
            "Le rotte in whitelist non devono richiedere il nonce.");
    }

    [Test]
    public async Task NonceMalformato_ShouldReturn500_Caratterizzazione()
    {
        // CARATTERIZZAZIONE, non approvazione: AesEncryption.DecryptString non gestisce l'input non
        // valido (Base64UrlDecode -> FormatException, oppure padding non valido -> CryptographicException),
        // quindi l'eccezione NON e' una SessionException e cade nel ramo "not null => 500" del gestore.
        // Effetto: un nonce corrotto (o tagliato da un proxy) diventa un errore server invece di una
        // sessione scaduta — rumore in App Insights e causa reale mascherata.
        // Se si decide di trattarlo come 419, e' questo test che va aggiornato (non cancellato).
        var client = _factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent("""{ "anno": 2024, "mese": 2 }""", Encoding.UTF8, "application/json");

        var resp = await client.PostAsync($"{RottaProtetta}?nonce=non-un-nonce-valido", content);
        TestContext.Out.WriteLine($"STATUS: {(int)resp.StatusCode} {resp.StatusCode}");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
            "Comportamento attuale: nonce malformato -> 500. V. commento: e' un difetto documentato, non l'esito desiderabile.");
    }
}
