using System.Net;
using System.Text;
using Microsoft.AspNetCore.Routing;
using PortaleFatture.BE.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.IntegrationTest.Http;

/// <summary>
/// Feature flag (Microsoft.FeatureManagement, v. FeatureFlags nell'API): con il flag assente la
/// funzionalita' e' spenta, le sue rotte non vengono registrate, quindi non compaiono in Swagger e
/// rispondono 404. Le rotte legacy SAP (api/fatture/invio/*, api/fatture/resetta/pipeline) hanno
/// il flag InvioSapLegacy; il resto di FattureModule non dipende da nessun flag.
///
/// Nei test i flag "spenti" valgono false in modo esplicito (v. ApiTestFactory), per non dipendere dagli
/// User Secrets della macchina; che un flag ASSENTE valga spento e' il default di Microsoft.FeatureManagement.
///
/// Per i 404 non serve il DB: arrivano dal routing, prima di qualunque handler.
/// </summary>
[Category("FeatureFlags")]
public class FeatureFlagsHttpTests
{
    private const string Swagger = "/swagger/v1/swagger.json";

    private ApiTestFactory _spente = null!;
    private ApiTestFactory _accese = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _spente = new ApiTestFactory([]);
        _accese = new ApiTestFactory(FeatureFlags.Tutti);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _spente?.Dispose();
        _accese?.Dispose();
    }

    private static async Task<HttpStatusCode> Post(ApiTestFactory factory, string rotta)
    {
        var client = factory.CreateClientAs(Ruolo.ADMIN);
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(factory.WithNonce(rotta), content);
        TestContext.Out.WriteLine($"{rotta}: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp.StatusCode;
    }

    private static async Task<HttpStatusCode> Get(ApiTestFactory factory, string rotta)
    {
        var client = factory.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.GetAsync(factory.WithNonce(rotta));
        TestContext.Out.WriteLine($"{rotta}: {(int)resp.StatusCode} {resp.StatusCode}");
        return resp.StatusCode;
    }

    private static Task<string> SwaggerJson(ApiTestFactory factory)
        => factory.CreateClient().GetStringAsync(Swagger);

    // --- flag assenti = spenti ---

    [TestCase("api/appio/contracts")]
    [TestCase("api/appio/financialreports/detail")]
    [TestCase("api/fatture/invio-sap")]
    [TestCase("api/fatture/invio-sap/dettaglio")]
    [TestCase("api/fatture/invio-sap/mesi")]
    [TestCase("api/fatture/invio-sap/tipologie")]
    [TestCase("api/fatture/invio/sap")]
    [TestCase("api/fatture/invio/pipeline")]
    [TestCase("api/fatture/resetta/pipeline")]
    public async Task FlagAssente_RotteNonRegistrate_404(string rotta)
        => Assert.That(await Post(_spente, rotta), Is.EqualTo(HttpStatusCode.NotFound));

    [TestCase("api/appio/contracts/tipoente")]
    [TestCase("api/fatture/invio-sap/lock")]
    [TestCase("api/fatture/invio-sap/stati")]
    [TestCase("api/fatture/invio-sap/anni")]
    [TestCase("api/fatture/invio/sap/multiplo")]
    public async Task FlagAssente_RotteGetNonRegistrate_404(string rotta)
        => Assert.That(await Get(_spente, rotta), Is.EqualTo(HttpStatusCode.NotFound));

    [Test]
    public async Task FlagAssente_RotteAssentiDaSwagger_RestoDiFatturePresente()
    {
        var json = await SwaggerJson(_spente);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Not.Contain("/api/appio/"), "APP IO spento: niente rotte in Swagger");
            Assert.That(json, Does.Not.Contain("/api/fatture/invio-sap"), "Invio a SAP spento: niente rotte in Swagger");
            Assert.That(json, Does.Not.Contain("/api/fatture/invio/"), "Invio a SAP legacy spento: niente rotte in Swagger");
            Assert.That(json, Does.Not.Contain("/api/fatture/resetta/pipeline"), "Invio a SAP legacy spento: niente rotte in Swagger");
            Assert.That(json, Does.Contain("/api/fatture/pagopa/whitelist"), "il resto di FattureModule resta registrato");
            Assert.That(json, Does.Contain("/api/fatture/stato"), "il resto di FattureModule resta registrato");
        });
    }

    // --- flag accesi ---

    [Test]
    public async Task FlagAccesi_RottePresentiInSwagger()
    {
        var json = await SwaggerJson(_accese);

        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("/api/appio/contracts"));
            Assert.That(json, Does.Contain("/api/appio/financialreports"));
            Assert.That(json, Does.Contain("/api/fatture/invio-sap"));
            Assert.That(json, Does.Contain("/api/fatture/invio/sap"));
            Assert.That(json, Does.Contain("/api/fatture/resetta/pipeline"));
        });
    }

    // --- le rotte senza flag restano registrate anche con tutti i flag spenti ---

    /// <summary>Rotte registrate dall'app (pattern + verbo), lette dalla tabella di routing.</summary>
    private static HashSet<string> Rotte(ApiTestFactory factory)
        => factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => $"{string.Join(",", e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])} /{e.RoutePattern.RawText?.TrimStart('/')}")
            .ToHashSet();

    private static bool SottoFlag(string rotta)
        => rotta.Contains(" /api/appio/")
        || rotta.Contains(" /api/fatture/invio-sap")
        || rotta.Contains(" /api/fatture/invio/")
        || rotta.Contains(" /api/fatture/resetta/pipeline");

    [Test]
    public void FlagSpenti_ShouldMancare_SoloLeRotteSottoFlag()
    {
        _spente.CreateClient().Dispose(); // avvia l'host
        _accese.CreateClient().Dispose();
        var spente = Rotte(_spente);
        var accese = Rotte(_accese);
        var mancanti = accese.Except(spente).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(spente, Is.SubsetOf(accese), "spegnere un flag non deve aggiungere rotte");
            Assert.That(mancanti, Has.All.Matches<string>(SottoFlag), "con i flag spenti manca una rotta che non e' sotto flag");
            Assert.That(accese.Where(SottoFlag), Is.SubsetOf(mancanti), "una rotta sotto flag e' registrata anche con il flag spento");
            Assert.That(mancanti, Has.Count.EqualTo(10 + 6 + accese.Count(r => r.Contains(" /api/appio/"))),
                "10 rotte Invio a SAP (dall'11/11/2026 anche anni, mesi e tipologie), 6 legacy e tutte quelle APP IO");
        });
    }

    /// <summary>
    /// Le rotte FUORI dai flag devono continuare a funzionare, non solo comparire in Swagger: ogni GET
    /// senza parametri di percorso (tutti i moduli tranne APP IO e le rotte SAP) viene chiamata con i
    /// flag spenti e con i flag accesi, e deve rispondere con lo stesso status. Se un flag toccasse per
    /// errore un'altra rotta, qui si vedrebbe un 404 solo da un lato.
    /// </summary>
    [Test]
    public async Task FlagSpenti_RotteFuoriDaiFlag_ShouldRispondere_ComeConIFlagAccesi()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        _spente.CreateClient().Dispose();
        _accese.CreateClient().Dispose();
        var rotte = Rotte(_accese)
            .Where(r => r.StartsWith("GET /api/") && !r.Contains('{') && !SottoFlag(r))
            .Select(r => r["GET /".Length..])
            .OrderBy(r => r)
            .ToList();
        TestContext.Out.WriteLine($"GET fuori dai flag: {rotte.Count}");

        var diverse = new List<string>();
        foreach (var rotta in rotte)
        {
            var spenta = await Get(_spente, rotta);
            var accesa = await Get(_accese, rotta);
            if (spenta != accesa)
                diverse.Add($"{rotta}: spenti {(int)spenta}, accesi {(int)accesa}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(rotte, Has.Count.GreaterThan(20), "il campione deve coprire tutti i moduli fuori dai flag");
            Assert.That(diverse, Is.Empty, "con i flag spenti una rotta fuori dai flag risponde in modo diverso");
        });
    }

    // --- ogni flag accende SOLO le sue rotte ---

    private static readonly Dictionary<string, Func<string, bool>> RotteDelFlag = new()
    {
        [FeatureFlags.AppIO] = r => r.Contains(" /api/appio/"),
        [FeatureFlags.InvioSap] = r => r.Contains(" /api/fatture/invio-sap"),
        [FeatureFlags.InvioSapLegacy] = r => r.Contains(" /api/fatture/invio/") || r.Contains(" /api/fatture/resetta/pipeline"),
    };

    [TestCaseSource(typeof(FeatureFlags), nameof(FeatureFlags.Tutti))]
    public void UnSoloFlagAcceso_ShouldRegistrare_SoloLeSueRotte(string flag)
    {
        using var factory = new ApiTestFactory([flag]);
        factory.CreateClient().Dispose();
        var sottoFlag = Rotte(factory).Where(SottoFlag).ToList();
        var attese = Rotte(_accese).Where(RotteDelFlag[flag]).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(attese, Is.Not.Empty);
            Assert.That(sottoFlag, Is.EquivalentTo(attese), $"con il solo {flag} acceso");
        });
    }

    [Test]
    public void RotteDelFlag_ShouldCoprire_TuttiIFlag()
        => Assert.That(RotteDelFlag.Keys, Is.EquivalentTo(FeatureFlags.Tutti), "un flag nuovo va aggiunto anche qui");

    // --- varianti del percorso: con il flag spento nessuna strada laterale raggiunge la rotta ---

    [TestCase("API/APPIO/CONTRACTS/TIPOENTE")]
    [TestCase("api/appio/contracts/tipoente/")]
    [TestCase("Api/Fatture/Invio-Sap/Stati")]
    [TestCase("api/fatture/invio-sap/stati/")]
    [TestCase("API/FATTURE/INVIO/SAP/MULTIPLO")]
    public async Task FlagSpenti_VariantiDelPercorso_404(string rotta)
        => Assert.That(await Get(_spente, rotta), Is.EqualTo(HttpStatusCode.NotFound));

    [TestCase("PUT", "api/fatture/invio-sap/genera")]
    [TestCase("PUT", "api/fatture/invio/sap/multiplo")]
    [TestCase("DELETE", "api/fatture/invio-sap")]
    [TestCase("OPTIONS", "api/appio/contracts")]
    public async Task FlagSpenti_AltriVerbi_404(string verbo, string rotta)
    {
        var client = _spente.CreateClientAs(Ruolo.ADMIN);
        var resp = await client.SendAsync(new HttpRequestMessage(new HttpMethod(verbo), _spente.WithNonce(rotta)));
        Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"{verbo} {rotta}");
    }

    // --- sonde usate nel testbook manuale (Insomnia, DEV): distinguono "rotta registrata" da "rotta spenta"
    //     senza dipendere dai dati: una POST senza body viene rifiutata prima dell'handler (400, o 500 sulle
    //     rotte che non gestiscono il body mancante), mentre una rotta non registrata da' 404. ---

    [TestCase("api/appio/financialreports/detail")]
    [TestCase("api/fatture/invio-sap/dettaglio")]
    [TestCase("api/fatture/invio/sap/multiplo/periodo")]
    public async Task Sonda_PostSenzaBody_FlagAccesoNon404_FlagSpento404(string rotta)
    {
        async Task<HttpStatusCode> SenzaBody(ApiTestFactory factory)
        {
            var client = factory.CreateClientAs(Ruolo.ADMIN);
            var resp = await client.PostAsync(factory.WithNonce(rotta), null);
            TestContext.Out.WriteLine($"{rotta} senza body: {(int)resp.StatusCode}");
            return resp.StatusCode;
        }

        Assert.Multiple(async () =>
        {
            Assert.That(await SenzaBody(_accese), Is.Not.EqualTo(HttpStatusCode.NotFound), "flag acceso");
            Assert.That(await SenzaBody(_spente), Is.EqualTo(HttpStatusCode.NotFound), "flag spento");
        });
    }

    [Test]
    public async Task FlagAcceso_RottaRegistrata_Non404()
        => Assert.That(await Get(_accese, "api/fatture/invio-sap/stati"), Is.Not.EqualTo(HttpStatusCode.NotFound));
}
