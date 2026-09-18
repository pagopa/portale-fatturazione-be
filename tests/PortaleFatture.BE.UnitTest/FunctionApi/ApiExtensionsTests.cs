using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// Comportamento atteso di ApiExtensions, il punto in cui la Integration API ricava dalla richiesta
/// HTTP le due cose su cui poggia TUTTA la sua autenticazione: la chiave (GetApiKey) e l'indirizzo
/// IP del chiamante (ExtractIpAddress). Gli endpoint sono AuthorizationLevel.Anonymous, quindi non
/// c'e' nessun controllo nativo di Azure Functions dietro a questo: se questi metodi sbagliano,
/// sbaglia l'autenticazione.
///
/// Qui sta il comportamento NORMALE. Gli input ostili — e i difetti che ne escono — stanno in
/// ApiExtensionsAdversarialTests.
/// </summary>
[TestFixture]
public class ApiExtensionsTests
{
    private const string UrlBase = "https://integration.uat.portalefatturazione.pagopa.it/api/v1/notifiche/periodo";

    private static FakeHttpRequestData Richiesta(string url = UrlBase) => new(new FakeFunctionContext(), url);

    // --- GetApiKey ----------------------------------------------------------------------------

    [Test]
    public void GetApiKey_ConHeaderPresente_ShouldRestituireIlValore()
    {
        var req = Richiesta().ConHeader("x-api-key", "CHIAVE-DI-TEST");

        Assert.That(req.GetApiKey(), Is.EqualTo("CHIAVE-DI-TEST"));
    }

    /// <summary>
    /// Il confronto sul nome dell'header e' OrdinalIgnoreCase: un client che scrive X-API-KEY o
    /// X-Api-Key deve essere autenticato lo stesso. HTTP non distingue il case nei nomi di header,
    /// quindi un confronto sensibile al case sarebbe un difetto.
    /// </summary>
    [TestCase("x-api-key")]
    [TestCase("X-API-KEY")]
    [TestCase("X-Api-Key")]
    public void GetApiKey_QualunqueCaseDellHeader_ShouldEssereRiconosciuto(string nomeHeader)
    {
        var req = Richiesta().ConHeader(nomeHeader, "CHIAVE-DI-TEST");

        Assert.That(req.GetApiKey(), Is.EqualTo("CHIAVE-DI-TEST"));
    }

    [Test]
    public void GetApiKey_SenzaHeader_ShouldRestituireNull()
    {
        Assert.That(Richiesta().GetApiKey(), Is.Null);
    }

    // --- SkipSwagger --------------------------------------------------------------------------

    /// <summary>
    /// SkipSwagger decide se saltare l'autenticazione: e' l'unico bypass previsto, e serve a lasciare
    /// pubblica la pagina di Swagger. Qui i due casi per cui esiste.
    /// </summary>
    [TestCase("https://host/api/swagger/ui")]
    [TestCase("https://host/api/swagger.json")]
    [TestCase("https://host/API/SWAGGER/ui")]
    public void SkipSwagger_SulleRotteDiSwagger_ShouldEssereTrue(string url)
    {
        Assert.That(Richiesta(url).SkipSwagger(), Is.True);
    }

    [TestCase("https://host/api/v1/notifiche/periodo")]
    [TestCase("https://host/api/v1/fatture/ricerca")]
    [TestCase("https://host/api/v1/authentication")]
    public void SkipSwagger_SulleRotteDiBusiness_ShouldEssereFalse(string url)
    {
        Assert.That(Richiesta(url).SkipSwagger(), Is.False);
    }

    // --- ExtractIpAddress ---------------------------------------------------------------------

    /// <summary>
    /// L'ordine di precedenza e' custom-forwarded-for, poi x-forwarded-for, poi x-original-for.
    /// Conta saperlo: e' l'header che il gateway deve valorizzare, ed e' anche quello che un client
    /// proverebbe a falsificare per aggirare la whitelist (v. il test adversarial corrispondente).
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConCustomForwardedFor_ShouldAvereLaPrecedenza()
    {
        var req = Richiesta()
            .ConHeader("custom-forwarded-for", "203.0.113.10")
            .ConHeader("x-forwarded-for", "203.0.113.20")
            .ConHeader("x-original-for", "203.0.113.30");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.10"));
    }

    [Test]
    public void ExtractIpAddress_SenzaCustomForwardedFor_ShouldUsareXForwardedFor()
    {
        var req = Richiesta()
            .ConHeader("x-forwarded-for", "203.0.113.20")
            .ConHeader("x-original-for", "203.0.113.30");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.20"));
    }

    [Test]
    public void ExtractIpAddress_SoloXOriginalFor_ShouldUsarloComeUltimaSpiaggia()
    {
        var req = Richiesta().ConHeader("x-original-for", "203.0.113.30");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.30"));
    }

    /// <summary>
    /// Un X-Forwarded-For attraversato da piu' proxy e' una lista: il primo elemento e' il client
    /// originale, gli altri sono gli hop. Si prende il primo.
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConListaDiHop_ShouldPrendereIlPrimo()
    {
        var req = Richiesta().ConHeader("x-forwarded-for", "203.0.113.10, 198.51.100.7, 192.0.2.1");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.10"));
    }

    /// <summary>
    /// Azure aggiunge la porta all'indirizzo (203.0.113.10:51234): va tolta, altrimenti il confronto
    /// con la whitelist non combacia mai.
    /// </summary>
    [Test]
    public void ExtractIpAddress_ConPorta_ShouldRimuoverla()
    {
        var req = Richiesta().ConHeader("x-forwarded-for", "203.0.113.10:51234");

        Assert.That(req.ExtractIpAddress(), Is.EqualTo("203.0.113.10"));
    }

    // --- GetUri -------------------------------------------------------------------------------

    /// <summary>
    /// GetUri riscrive l'URL di polling che Durable Functions genera sull'host interno, sostituendogli
    /// il dominio pubblico: senza, il chiamante riceverebbe uno statusQueryGetUri che dal suo lato
    /// della rete non e' raggiungibile. Il "/api" finale del dominio configurato viene tolto perche'
    /// il path lo contiene gia'.
    /// </summary>
    [Test]
    public void GetUri_ShouldSostituireSchemaEHostConIlDominioPubblico()
    {
        var configurazione = new FakeConfigurazione { CustomDomain = "https://integration.uat.portalefatturazione.pagopa.it/api" };

        var risultato = configurazione.GetUri("http://localhost:7071/runtime/webhooks/durabletask/instances/abc?code=xyz");

        Assert.That(risultato, Is.EqualTo(
            "https://integration.uat.portalefatturazione.pagopa.it/runtime/webhooks/durabletask/instances/abc?code=xyz"));
    }

    [Test]
    public void GetUri_ShouldLasciareIntattiPathEQueryString()
    {
        var configurazione = new FakeConfigurazione { CustomDomain = "https://pubblico.example/api" };

        var risultato = configurazione.GetUri("https://interno.local/a/b/c?x=1&y=2");

        Assert.That(risultato, Is.EqualTo("https://pubblico.example/a/b/c?x=1&y=2"));
    }
}
