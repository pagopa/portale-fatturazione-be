using System.Reflection;
using System.Security;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using PortaleFatture.BE.Api.Modules.Fatture;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.UnitTest;

public class GestioneFattureEndpointUnitTests
{
    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture mappi correttamente i filtri 
    /// e la paginazione, e ritorni un risultato Ok con il DTO atteso.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneAsync_ShouldMapFiltersAndPaging_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFatture
        {
            Anno = 2026,
            IdEnti = new[] { "ENTE-1", "ENTE-2" },
            Mesi = new[] { 2, 3 },
            TipologiaContratto = 7,
            TipologiaFattura = "SECONDO SALDO",
            Azione = "POSTICIPA",
            Note = "nota test"
        };

        var dto = new GestioneFattureListDto
        {
            GestioneFatture =
            [
                new SimpleGestioneFattureDto { Ente = "ENTE-1", Anno = 2026, Mese = 2, Azione = "POSTICIPA" }
            ],
            Count = 1
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var endpointResult = await InvokePostGestioneFattureAsync(module, context, request, page: 2, pageSize: 20, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureQuery>(q =>
                q.Anno == request.Anno &&
                q.IdEnti != null && q.IdEnti.SequenceEqual(request.IdEnti!) &&
                q.Mesi != null && q.Mesi.SequenceEqual(request.Mesi!) &&
                q.TipologiaContratto == request.TipologiaContratto &&
                q.TipologiaFattura == request.TipologiaFattura &&
                q.Azione == request.Azione &&
                q.Note == request.Note &&
                q.Page == 2 &&
                q.Size == 20),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture ritorni NotFound 
    /// quando il layer di query ritorna null.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFatture { Anno = 2026, Mesi = new[] { 2 } };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GestioneFattureListDto?)null);

        var endpointResult = await InvokePostGestioneFattureAsync(module, context, request, page: 1, pageSize: 10, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture ritorni NotFound 
    /// quando il layer di query ritorna un DTO con lista vuota.
    /// </summary> 
    public async Task PostPagoPAGestioneFatturazioneAsync_WhenListIsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFatture { Anno = 2026, Mesi = new[] { 2 } };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GestioneFattureListDto { GestioneFatture = Array.Empty<SimpleGestioneFattureDto>(), Count = 0 });

        var endpointResult = await InvokePostGestioneFattureAsync(module, context, request, page: 1, pageSize: 10, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che la classe RicercaGestioneFatture normalizzi gli array vuoti a null, 
    /// per evitare problemi di serializzazione e query.
    /// </summary>
    public void RicercaGestioneFatture_WhenArraysAreEmpty_ShouldNormalizeToNull()
    {
        var request = new RicercaGestioneFatture
        {
            IdEnti = Array.Empty<string>(),
            Mesi = Array.Empty<int>()
        };

        Assert.That(request.IdEnti, Is.Null);
        Assert.That(request.Mesi, Is.Null);
    }

    [Test]
    /// <summary>
    /// Verifica che la classe GestioneFattureQuery normalizzi gli array vuoti a null, 
    /// per evitare problemi di serializzazione e query.
    /// </summary>
    public void GestioneFattureQuery_WhenIdEntiIsEmpty_ShouldNormalizeToNull()
    {
        var query = new GestioneFattureQuery(new AuthenticationInfo())
        {
            IdEnti = Array.Empty<string>()
        };

        Assert.That(query.IdEnti, Is.Null);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint non intercetti una richiesta non autenticata e propaghi la SecurityException
    /// generata da GetAuthInfo().
    /// </summary>
    public void PostPagoPAGestioneFatturazioneAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // identity non autenticata
        };
        var request = new RicercaGestioneFatture { Anno = 2026 };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureAsync(module, context, request, page: 1, pageSize: 10, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica il comportamento attuale: page/pageSize non validi vengono inoltrati al layer query
    /// senza validazione nel metodo endpoint.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneAsync_ShouldForwardInvalidPagingValues_AsIs()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFatture { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GestioneFattureListDto?)null);

        var endpointResult = await InvokePostGestioneFattureAsync(module, context, request, page: 0, pageSize: 0, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureQuery>(q => q.Page == 0 && q.Size == 0),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }


    /// <summary>
    /// Costruisce un HttpContext con un utente autenticato e i claim necessari per l'endpoint.
    /// </summary>
    private static DefaultHttpContext BuildAuthenticatedHttpContext()
    {
        var claims = new List<Claim>
        {
            new(CustomClaim.DescrizioneRuolo, "Admin"),
            new(CustomClaim.IdEnte, "ENTE-TEST"),
            new(CustomClaim.Prodotto, "prod-pn"),
            new(CustomClaim.Profilo, "admin"),
            new(ClaimTypes.Role, Ruolo.ADMIN),
            new(ClaimTypes.Name, "user-test"),
            new(CustomClaim.NomeEnte, "Ente Test"),
            new(CustomClaim.GruppoRuolo, "grp"),
            new(CustomClaim.Auth, "auth"),
            new(CustomClaim.IdTipoContratto, "1")
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        return context;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture tramite reflection,
    /// passando i parametri necessari e restituendo il risultato dell'endpoint.
    /// </summary>
    /// <param name="module">Il modulo che contiene l'endpoint.</param>
    /// <param name="context">Il contesto HTTP autenticato.</param>
    /// <param name="request">La richiesta da inviare all'endpoint.</param>
    /// <param name="page">Il numero di pagina per la paginazione.</param>
    /// <param name="pageSize">La dimensione della pagina per la paginazione.</param>
    /// <param name="mediator">Il mediatore da utilizzare per invocare la query.</param>
    /// <returns>Il risultato dell'endpoint.</returns>
    private static async Task<object> InvokePostGestioneFattureAsync(
        FattureModule module,
        HttpContext context,
        RicercaGestioneFatture request,
        int page,
        int pageSize,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "PostPagoPAGestioneFatturazioneAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, page, pageSize, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Estrae il risultato interno di un oggetto endpoint, che può essere un IActionResult 
    /// o un altro tipo di risultato, per facilitare le asserzioni nei test.
    /// </summary>
    private static object? GetInnerResult(object endpointResult)
    {
        return endpointResult.GetType().GetProperty("Result")?.GetValue(endpointResult);
    }
}