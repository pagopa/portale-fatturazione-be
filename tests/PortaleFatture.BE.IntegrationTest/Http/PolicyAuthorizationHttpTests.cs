using System.Net;
using System.Text;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Separazione fra i due mondi del portale, provata end-to-end con token REALI (JwtApiTestFactory).
///
/// Le due policy leggono claim diversi dello STESSO token: `SelfCarePolicy` pretende
/// `auth = SELFCARE` piu' un `profilo` fra quelli ammessi (PA, GSP, SCP, PSP, AS, SA, PT);
/// `PagoPAPolicy` pretende `auth = PAGOPA`. Il ruolo (OPERATOR/ADMIN) e' un asse ortogonale.
/// Sono claim che viaggiano dentro il JWT, quindi questi test coprono due rischi in uno: la
/// separazione di visibilita' fra aderente e pannello interno, e il fatto che quei claim
/// sopravvivano al giro di (de)serializzazione del token.
///
/// I casi che contano di piu' sono gli INCROCI (token di un mondo su rotta dell'altro): devono dare
/// 403 — cioe' "identita' riconosciuta ma non autorizzata". Un 401 direbbe che il token non e' stato
/// nemmeno letto, un 200 sarebbe un problema di visibilita' dei dati, non un dettaglio tecnico.
/// Gli incroci non toccano il DB: l'autorizzazione decide prima che l'endpoint venga invocato.
///
/// Nota sui test unit: le policy sono dichiarate una volta sola in AddJwtOrApiKeyAuthentication e
/// valutate solo dal middleware; un unit test dovrebbe ridichiararle, verificando la propria copia
/// invece del prodotto. Per questo la copertura vive qui.
/// </summary>
public class PolicyAuthorizationHttpTests
{
    // Rotte rappresentative delle quattro policy (nessuna in whitelist del nonce: v. WithNonce).
    private const string RottaEnte = "/api/fatture/ente";                                 // SelfCarePolicy
    private const string RottaAdmin = "/api/fatture";                                     // PagoPAPolicy
    private const string RottaConsolidatore = "/api/tipologia/enti/consolidatore/completi"; // SelfCareConsolidatorePolicy

    private JwtApiTestFactory _factory;

    [OneTimeSetUp]
    public void Setup() => _factory = new JwtApiTestFactory();

    [SetUp]
    public void CheckConfigurazione() => _factory.SkipSeConfigurazioneJwtAssente();

    [OneTimeTearDown]
    public void TearDown() => _factory?.Dispose();

    private async Task<HttpResponseMessage> Post(string rotta, string token, string body = "{}")
    {
        var client = _factory.CreateClientWithToken(token);
        var resp = await client.PostAsync(
            _factory.WithNonce(rotta),
            new StringContent(body, Encoding.UTF8, "application/json"));

        TestContext.Out.WriteLine($"{rotta} -> {(int)resp.StatusCode} {resp.StatusCode}");
        return resp;
    }

    // Profilo espone campi static, non const: il default si risolve nel corpo, non nella firma.
    private string TokenEnte(string ruolo = Ruolo.ADMIN, string? profilo = null) =>
        _factory.Token(ruolo: ruolo, auth: AuthType.SELFCARE, profilo: profilo ?? Profilo.PubblicaAmministrazione);

    private string TokenAdmin(string ruolo = Ruolo.ADMIN) =>
        _factory.Token(ruolo: ruolo, auth: AuthType.PAGOPA, profilo: Profilo.Approvigionamento);

    // --- lato Ente (SelfCarePolicy) -------------------------------------------------------------

    /// <summary>
    /// Accettato dalla policy: si asserisce "ne' 401 ne' 403", non l'esito della ricerca, che dipende
    /// dai dati del DB seedato.
    /// </summary>
    [Test]
    public async Task TokenEnte_SuRottaEnte_ShouldPassAuthorization()
    {
        var resp = await Post(RottaEnte, TokenEnte(), """{ "anno": 2024, "mese": 2 }""");

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized)
                                       .And.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Il claim `profilo` non e' decorativo: un valore fuori dalla whitelist della policy blocca
    /// l'accesso anche con `auth = SELFCARE` corretto.
    /// </summary>
    [Test]
    public async Task TokenEnte_ConProfiloNonAmmesso_SuRottaEnte_ShouldReturn403()
    {
        var resp = await Post(RottaEnte, TokenEnte(profilo: "PROFILO-INESISTENTE"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // --- lato Admin (PagoPAPolicy) --------------------------------------------------------------

    [Test]
    public async Task TokenAdmin_SuRottaAdmin_ShouldPassAuthorization()
    {
        var resp = await Post(RottaAdmin, TokenAdmin(), """{ "anno": 2024, "mese": 2 }""");

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized)
                                       .And.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    // --- incroci: sono questi i casi che dicono se la separazione regge ---------------------------

    [Test]
    public async Task TokenEnte_SuRottaAdmin_ShouldReturn403()
    {
        var resp = await Post(RottaAdmin, TokenEnte(), """{ "anno": 2024, "mese": 2 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
            "Un token SelfCare non deve poter leggere le rotte del pannello interno.");
    }

    [Test]
    public async Task TokenAdmin_SuRottaEnte_ShouldReturn403()
    {
        var resp = await Post(RottaEnte, TokenAdmin(), """{ "anno": 2024, "mese": 2 }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
            "Un token pagoPA non deve entrare dalle rotte pensate per l'aderente.");
    }

    // --- profili speciali SelfCare ---------------------------------------------------------------

    [Test]
    public async Task TokenConsolidatore_SuRottaConsolidatore_ShouldPassAuthorization()
    {
        var resp = await Post(
            RottaConsolidatore,
            TokenEnte(profilo: Profilo.Consolidatore),
            """{ "descrizione": "test" }""");

        Assert.That(resp.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized)
                                       .And.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// Le policy specializzate non sono un sottoinsieme di SelfCarePolicy: un aderente PA, pur essendo
    /// SelfCare a tutti gli effetti, non deve entrare nelle rotte del Consolidatore.
    /// </summary>
    [Test]
    public async Task TokenEntePA_SuRottaConsolidatore_ShouldReturn403()
    {
        var resp = await Post(RottaConsolidatore, TokenEnte(), """{ "descrizione": "test" }""");

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // --- ruolo: asse ortogonale alla policy ------------------------------------------------------

    /// <summary>
    /// Il ruolo viaggia nello stesso token ma e' verificato da [Authorize(Roles=...)], non dalla
    /// policy: un ruolo inesistente deve fermare la richiesta anche quando la policy e' soddisfatta.
    /// </summary>
    [Test]
    public async Task TokenEnte_ConRuoloNonAmmesso_ShouldReturn403()
    {
        var resp = await Post(RottaEnte, TokenEnte(ruolo: "RUOLO-INESISTENTE"));

        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }
}
