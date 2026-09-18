using System.Net;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PortaleFatture.BE.Function.API.Middleware;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;

namespace PortaleFatture.BE.UnitTest.FunctionApi;

/// <summary>
/// AuthMiddleware montato per davvero: è l'UNICA autenticazione della Integration API esposta ai
/// grandi aderenti. Gli HttpTrigger sono tutti AuthorizationLevel.Anonymous, quindi dietro a questo
/// middleware non c'è nessun altro controllo — né quello nativo a chiave di Azure Functions, né un
/// API gateway (v. docs/autenticazione.md: la validazione di chiave e IP è fatta qui, contro il DB).
///
/// Il middleware non è banale da montare: GetHttpRequestDataAsync cerca IHttpRequestDataFeature fra
/// le feature dell'invocazione e solo in sua assenza ripiega su IFunctionBindingsFeature, che nel
/// SDK è **internal** e quindi non implementabile da un test. È il motivo per cui i fake forniscono
/// la prima e non la seconda — senza quel dettaglio l'area non sarebbe testabile affatto.
///
/// Le query verso il DB sono sostituite da un IMediator finto: qui si verifica la LOGICA DI
/// DECISIONE del middleware. Le due query reali sono coperte su DB seedato da
/// ApiKeysQueryIntegrationTests.
/// </summary>
[TestFixture]
public class AuthMiddlewareTests
{
    private const string RottaBusiness = "https://integration.uat.portalefatturazione.pagopa.it/api/v1/notifiche/periodo";
    private const string ChiaveValida = "CHIAVE-BUONA";
    private const string IdEnteDellaChiave = "11111111-1111-1111-1111-111111111111";
    private const string IpAutorizzato = "203.0.113.10";

    private Mock<IMediator> _mediator;
    private bool _nextChiamato;

    [SetUp]
    public void Setup()
    {
        _mediator = new Mock<IMediator>();
        _nextChiamato = false;
    }

    // ---------------------------------------------------------------------------------------------
    // Il bypass
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 IL CASO CHE CONTA. SkipSwagger cerca "/api/swagger" come sottostringa dell'URL intero,
    /// query string compresa, invece di guardare il solo path (v. ApiExtensionsAdversarialTests).
    /// Qui si verifica la CONSEGUENZA sul middleware, che è ciò che davvero importa: una richiesta a
    /// una rotta di business, SENZA alcuna chiave API, con quella stringa in un parametro di query
    /// **arriva alla function**.
    ///
    /// Nessuna risposta di rifiuto viene prodotta, e nessuna delle due query di autenticazione viene
    /// nemmeno interrogata: non serve conoscere una chiave, né essere in whitelist.
    ///
    /// Aggravante: anche LogCustomDataMiddleware usa lo stesso SkipSwagger, quindi una richiesta
    /// così non viene neppure tracciata in pfw.ApiLog.
    ///
    /// Cosa questo test NON dice: quali dati escano. A valle IdEnte resta null e l'effetto dipende
    /// da come ogni singola activity lo tratta. Ma il salto dei controlli è qui, dimostrato.
    ///
    /// Caratterizzazione: quando SkipSwagger guarderà il path, questo diventa rosso — ed è il
    /// segnale che il difetto è chiuso. Va allora riscritto attendendosi 401.
    /// </summary>
    [TestCase(RottaBusiness + "?x=/api/swagger")]
    [TestCase(RottaBusiness + "?redirect=/api/swagger.json")]
    [TestCase("https://host/api/v1/contestazioni/api/swagger/upload")]
    public async Task Invoke_RottaDiBusinessConLaStringaSwaggerNellUrl_PassaSenzaAutenticazione(string url)
    {
        var (contesto, richiesta) = Contesto(url); // nessun header x-api-key

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.True,
                "La richiesta è arrivata alla function senza chiave API né controllo dell'IP.");
            Assert.That(richiesta.RisposteCreate, Is.Empty, "Nessun rifiuto è stato prodotto.");
        });
        _mediator.Verify(m => m.Send(It.IsAny<ApiKeyQueryGetByKeyEnte>(), It.IsAny<CancellationToken>()), Times.Never,
            "La chiave non è stata nemmeno cercata.");
    }

    /// <summary>
    /// Contro-prova: sulla rotta di Swagger vera il salto è legittimo e voluto (la pagina deve
    /// restare pubblica). Serve a delimitare il difetto: il problema non è che SkipSwagger esista,
    /// è come riconosce la rotta.
    /// </summary>
    [Test]
    public async Task Invoke_SullaRottaDiSwagger_PassaSenzaAutenticazione_EdECorretto()
    {
        var (contesto, _) = Contesto("https://host/api/swagger/ui");

        await Middleware().Invoke(contesto, Next);

        Assert.That(_nextChiamato, Is.True);
    }

    // ---------------------------------------------------------------------------------------------
    // I rifiuti
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Invoke_SenzaChiaveApi_ShouldRispondere401_ESenzaProseguire()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness);

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.False, "La function NON deve essere raggiunta.");
            Assert.That(StatoRisposta(richiesta), Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    [Test]
    public async Task Invoke_ConChiaveSconosciuta_ShouldRispondere401()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness, ("x-api-key", "CHIAVE-INVENTATA"));
        ChiaveRisolveA(null);

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.False);
            Assert.That(StatoRisposta(richiesta), Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    /// <summary>
    /// La query reale non restituisce null per una chiave sconosciuta: LANCIA (SingleAsync su zero
    /// righe — v. ApiKeysQueryIntegrationTests). Il middleware regge perché IsValidApiKey ha un
    /// try/catch che converte qualunque eccezione in null, quindi in 401.
    ///
    /// Il test fissa questa dipendenza: è l'unica cosa che impedisce a un errore del DB di diventare
    /// un 500 su una rotta pubblica. ⚠️ Rovescio della medaglia, verificato leggendo il codice: un
    /// guasto vero del database (connessione caduta, timeout) viene mascherato allo stesso modo, e
    /// l'aderente riceve "Invalid or missing API Key" mentre il problema è nostro.
    /// </summary>
    [Test]
    public async Task Invoke_SeLaQueryDellaChiaveLancia_ShouldRispondere401_NonPropagareLEccezione()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness, ("x-api-key", ChiaveValida));
        _mediator.Setup(m => m.Send(It.IsAny<ApiKeyQueryGetByKeyEnte>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Sequence contains no elements"));

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.False);
            Assert.That(StatoRisposta(richiesta), Is.EqualTo(HttpStatusCode.Unauthorized));
        });
    }

    [Test]
    public async Task Invoke_ConChiaveValidaMaNessunIpInWhitelist_ShouldRispondere403()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness, ("x-api-key", ChiaveValida), ("x-forwarded-for", IpAutorizzato));
        ChiaveRisolveA(DtoChiave());
        IpAutorizzatiSono();

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.False);
            Assert.That(StatoRisposta(richiesta), Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    [Test]
    public async Task Invoke_ConIpNonInWhitelist_ShouldRispondere403()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness, ("x-api-key", ChiaveValida), ("x-forwarded-for", "198.51.100.7"));
        ChiaveRisolveA(DtoChiave());
        IpAutorizzatiSono(IpAutorizzato);

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.False);
            Assert.That(StatoRisposta(richiesta), Is.EqualTo(HttpStatusCode.Forbidden));
        });
    }

    // ---------------------------------------------------------------------------------------------
    // Il percorso felice, e cosa lascia in eredità alla function
    // ---------------------------------------------------------------------------------------------

    [Test]
    public async Task Invoke_ConChiaveValidaEIpAutorizzato_ShouldProseguire()
    {
        var (contesto, richiesta) = Contesto(RottaBusiness, ("x-api-key", ChiaveValida), ("x-forwarded-for", IpAutorizzato));
        ChiaveRisolveA(DtoChiave());
        IpAutorizzatiSono(IpAutorizzato);

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextChiamato, Is.True);
            Assert.That(richiesta.RisposteCreate, Is.Empty);
        });
    }

    [Test]
    public async Task Invoke_ConIpCopertoDaUnCidr_ShouldProseguire()
    {
        var (contesto, _) = Contesto(RottaBusiness, ("x-api-key", ChiaveValida), ("x-forwarded-for", "203.0.113.77"));
        ChiaveRisolveA(DtoChiave());
        IpAutorizzatiSono("203.0.113.0/24");

        await Middleware().Invoke(contesto, Next);

        Assert.That(_nextChiamato, Is.True);
    }

    /// <summary>
    /// 🔒 Isolamento fra aderenti. L'IdEnte che ogni activity userà per filtrare i dati viene messo
    /// qui in FunctionContext.Items, e viene preso dalla RISOLUZIONE DELLA CHIAVE — non da qualcosa
    /// che il chiamante possa influenzare.
    ///
    /// Il test lo dimostra mandando header che provano a suggerire un altro ente: l'IdEnte che resta
    /// nel contesto è quello della chiave. È la proprietà su cui poggia tutta la separazione fra
    /// aderenti nella Integration API.
    /// </summary>
    [Test]
    public async Task Invoke_LIdEnteNelContesto_VieneDallaChiave_NonDaHeaderDelChiamante()
    {
        var (contesto, _) = Contesto(RottaBusiness,
            ("x-api-key", ChiaveValida),
            ("x-forwarded-for", IpAutorizzato),
            ("idente", "99999999-9999-9999-9999-999999999999"),
            ("x-idente", "99999999-9999-9999-9999-999999999999"));
        ChiaveRisolveA(DtoChiave());
        IpAutorizzatiSono(IpAutorizzato);

        await Middleware().Invoke(contesto, Next);

        Assert.Multiple(() =>
        {
            Assert.That(contesto.Items["IdEnte"], Is.EqualTo(IdEnteDellaChiave));
            Assert.That(contesto.Items["RagioneSociale"], Is.EqualTo("Ente Di Prova"));
            Assert.That(contesto.Items["IdContratto"], Is.EqualTo("TOKEN-E1"));
        });
    }

    /// <summary>
    /// Un'invocazione che non è un httpTrigger — cioè un'activity o un orchestrator di Durable
    /// Functions — passa senza autenticazione, ed è corretto: non arriva dalla rete, la innesca
    /// l'orchestrazione già autenticata a monte. Va però saputo, perché significa che il presidio
    /// esiste solo sul primo salto.
    /// </summary>
    [Test]
    public async Task Invoke_SuUnaActivityNonHttp_ShouldProseguireSenzaAutenticare()
    {
        var (contesto, _) = Contesto(RottaBusiness, tipoBinding: "activityTrigger");

        await Middleware().Invoke(contesto, Next);

        Assert.That(_nextChiamato, Is.True);
    }

    // ---------------------------------------------------------------------------------------------

    private AuthMiddleware Middleware() => new(
        NullLogger<AuthMiddleware>.Instance,
        new FakeConfigurazione { CustomDomain = "https://integration.uat.portalefatturazione.pagopa.it/api" },
        _mediator.Object);

    private Task Next(FunctionContext _)
    {
        _nextChiamato = true;
        return Task.CompletedTask;
    }

    private static (FakeFunctionContext, FakeHttpRequestData) Contesto(
        string url, params (string Nome, string Valore)[] headers) => Contesto(url, "httpTrigger", headers);

    private static (FakeFunctionContext, FakeHttpRequestData) Contesto(
        string url, string tipoBinding, params (string Nome, string Valore)[] headers)
    {
        var contesto = new FakeFunctionContext
        {
            DefinizioneImpostata = new FakeFunctionDefinition("req", tipoBinding)
        };

        var richiesta = new FakeHttpRequestData(contesto, url);
        foreach (var (nome, valore) in headers)
            richiesta.ConHeader(nome, valore);

        var feature = new FakeInvocationFeatures();
        feature.Set<IHttpRequestDataFeature>(new FakeHttpRequestDataFeature(richiesta));
        contesto.FeatureImpostate = feature;

        return (contesto, richiesta);
    }

    private void ChiaveRisolveA(ApiKeyEnteDto? dto) =>
        _mediator.Setup(m => m.Send(It.IsAny<ApiKeyQueryGetByKeyEnte>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(dto);

    private void IpAutorizzatiSono(params string[] indirizzi) =>
        _mediator.Setup(m => m.Send(It.IsAny<ApiKeyIpsQueryGet>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(indirizzi.Select(ip => new ApiKeyIpsDto { IpAddress = ip }).ToList());

    private static ApiKeyEnteDto DtoChiave() => new()
    {
        IdEnte = IdEnteDellaChiave,
        ApiKey = ChiaveValida,
        RagioneSociale = "Ente Di Prova",
        IdContratto = "TOKEN-E1",
        IdTipoContratto = 2,
        Prodotto = "prod-pn",
        Profilo = "PA"
    };

    private static HttpStatusCode StatoRisposta(FakeHttpRequestData richiesta)
    {
        Assert.That(richiesta.RisposteCreate, Is.Not.Empty, "Il middleware non ha prodotto alcuna risposta.");
        return richiesta.RisposteCreate[^1].StatusCode;
    }
}
