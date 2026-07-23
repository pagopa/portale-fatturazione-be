using System.Reflection;
using System.Security;
using System.Security.Claims;
using System.Collections;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using PortaleFatture.BE.Api.Modules.Fatture;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
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
    /// Verifica che la request download normalizzi gli array vuoti a null.
    /// </summary>
    public void RicercaGestioneFattureDownloadRequest_WhenArraysAreEmpty_ShouldNormalizeToNull()
    {
        var request = new RicercaGestioneFattureDownloadRequest
        {
            IdEnti = Array.Empty<string>(),
            Mesi = Array.Empty<int>()
        };

        Assert.That(request.IdEnti, Is.Null);
        Assert.That(request.Mesi, Is.Null);
    }

    [Test]
    /// <summary>
    /// Verifica che la query download normalizzi gli array vuoti a null.
    /// </summary>
    public void GestioneFattureDownloadQuery_WhenArraysAreEmpty_ShouldNormalizeToNull()
    {
        var query = new GestioneFattureDownloadQuery(new AuthenticationInfo())
        {
            IdEnti = Array.Empty<string>(),
            Mesi = Array.Empty<int>()
        };

        Assert.That(query.IdEnti, Is.Null);
        Assert.That(query.Mesi, Is.Null);
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

    [Test]
    /// <summary>
    /// Verifica che l'endpoint GET /api/fatture/pagopa/gestione-fatture/anni ritorni Ok
    /// e inoltri al mediator una GestioneFattureAnniQuery con auth info valorizzata.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneAnniAsync_ShouldReturnOk_AndForwardAuthInfo()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 2026, 2025 });

        var endpointResult = await InvokeGetGestioneFattureAnniAsync(module, context, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureAnniQuery>(q =>
                q.AuthenticationInfo != null &&
                q.AuthenticationInfo.Ruolo == Ruolo.ADMIN &&
                q.AuthenticationInfo.IdEnte == "ENTE-TEST" &&
                q.AuthenticationInfo.Prodotto == "prod-pn"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint GET /api/fatture/pagopa/gestione-fatture/anni ritorni NotFound
    /// quando il layer query ritorna null.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneAnniAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int>?)null);

        var endpointResult = await InvokeGetGestioneFattureAnniAsync(module, context, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint GET /api/fatture/pagopa/gestione-fatture/anni ritorni NotFound
    /// quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneAnniAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var endpointResult = await InvokeGetGestioneFattureAnniAsync(module, context, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint GET /api/fatture/pagopa/gestione-fatture/anni propaghi
    /// la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void GetPagoPAGestioneFatturazioneAnniAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokeGetGestioneFattureAnniAsync(module, context, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi
    /// inoltri correttamente l'anno alla query e ritorni Ok con mesi mappati.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneMesiAsync_ShouldMapAnno_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureMesiRequest { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 2, 12 });

        var endpointResult = await InvokePostGestioneFattureMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureMesiQuery>(q =>
                q.Anno == request.Anno &&
                q.AuthenticationInfo != null &&
                q.AuthenticationInfo.Ruolo == Ruolo.ADMIN),
            It.IsAny<CancellationToken>()),
            Times.Once);

        var value = innerResult.GetType().GetProperty("Value")?.GetValue(innerResult);
        Assert.That(value, Is.Not.Null);

        var items = ((IEnumerable)value!).Cast<object>().ToList();
        Assert.That(items.Count, Is.EqualTo(2));

        var mesi = items
            .Select(x => x.GetType().GetProperty("Mese")?.GetValue(x)?.ToString())
            .ToList();

        var descrizioni = items
            .Select(x => x.GetType().GetProperty("Descrizione")?.GetValue(x)?.ToString())
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(mesi, Is.EqualTo(new[] { "2", "12" }));
            Assert.That(descrizioni.All(d => !string.IsNullOrWhiteSpace(d)), Is.True);
        });
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi ritorni NotFound
    /// quando il layer query ritorna null.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneMesiAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureMesiRequest { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int>?)null);

        var endpointResult = await InvokePostGestioneFattureMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi ritorni NotFound
    /// quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task PostPagoPAGestioneFatturazioneMesiAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureMesiRequest { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var endpointResult = await InvokePostGestioneFattureMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi propaghi
    /// la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void PostPagoPAGestioneFatturazioneMesiAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureMesiAsync(module, context, new GestioneFattureMesiRequest { Anno = 2026 }, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/anni
    /// inoltri correttamente TipologiaFattura/Azione alla query e ritorni Ok.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneAnniAsync_ShouldMapFilters_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 2026, 2025 });

        var endpointResult = await InvokePostGestioneFattureModificaAnniAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureModificaAnniQuery>(q =>
                q.AuthenticationInfo != null &&
                q.TipologiaFattura == request.TipologiaFattura &&
                q.Azione == request.Azione),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/anni ritorni NotFound
    /// quando il layer query ritorna null.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneAnniAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int>?)null);

        var endpointResult = await InvokePostGestioneFattureModificaAnniAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/anni ritorni NotFound
    /// quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneAnniAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaAnniQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var endpointResult = await InvokePostGestioneFattureModificaAnniAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/anni propaghi
    /// la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void GetPagoPAModificaGestioneFatturazioneAnniAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var request = new GestioneFattureModificaRequest { TipologiaFattura = "SECONDO SALDO", Azione = "ELIMINA" };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureModificaAnniAsync(module, context, request, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/mesi
    /// inoltri correttamente Anno/TipologiaFattura/Azione e ritorni Ok con mesi mappati.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneMesiAsync_ShouldMapFilters_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            Anno = "2026",
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { 2, 12 });

        var endpointResult = await InvokePostGestioneFattureModificaMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureModificaMesiQuery>(q =>
                q.AuthenticationInfo != null &&
                q.Anno == request.Anno &&
                q.TipologiaFattura == request.TipologiaFattura &&
                q.Azione == request.Azione),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/mesi ritorni NotFound
    /// quando il layer query ritorna null.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneMesiAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            Anno = "2026",
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<int>?)null);

        var endpointResult = await InvokePostGestioneFattureModificaMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/mesi ritorni NotFound
    /// quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task GetPagoPAModificaGestioneFatturazioneMesiAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureModificaRequest
        {
            Anno = "2026",
            TipologiaFattura = "SECONDO SALDO",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureModificaMesiQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<int>());

        var endpointResult = await InvokePostGestioneFattureModificaMesiAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/mesi propaghi
    /// la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void GetPagoPAModificaGestioneFatturazioneMesiAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var request = new GestioneFattureModificaRequest { Anno = "2026", TipologiaFattura = "SECONDO SALDO", Azione = "ELIMINA" };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureModificaMesiAsync(module, context, request, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/download
    /// mappi correttamente i filtri e ritorni uno stream Excel.
    /// </summary>
    public async Task PostPagoPAGestioneFattureDownloadAsync_ShouldMapFilters_AndReturnExcelStream()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFattureDownloadRequest
        {
            Anno = 2026,
            IdEnti = new[] { "ENTE-1", "ENTE-2" },
            Mesi = new[] { 2, 3 },
            TipologiaContratto = 7,
            TipologiaFattura = "SECONDO SALDO"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureDownloadQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GestioneFattureListDto
            {
                GestioneFatture =
                [
                    new SimpleGestioneFattureDto
                    {
                        Ente = "ENTE-1",
                        RagioneSociale = "Ente 1",
                        Anno = 2026,
                        Mese = 2,
                        TipologiaFattura = "SECONDO SALDO",
                        IdTipoContratto = 7,
                        TipoContratto = "7",
                        Azione = "POSTICIPA"
                    }
                ],
                Count = 1
            });

        var result = await InvokePostGestioneFattureDownloadAsync(module, context, request, mediator.Object);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Does.Contain("Stream"));

        var contentType = result.GetType().GetProperty("ContentType")?.GetValue(result)?.ToString();
        var fileDownloadName = result.GetType().GetProperty("FileDownloadName")?.GetValue(result)?.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(contentType, Is.EqualTo("application/vnd.ms-excel"));
            Assert.That(fileDownloadName, Is.Not.Null);
            Assert.That(fileDownloadName, Does.EndWith(".xlsx"));
        });

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureDownloadQuery>(q =>
                q.AuthenticationInfo != null &&
                q.AuthenticationInfo.Id == "user-test" &&
                q.Anno == request.Anno &&
                q.IdEnti != null && q.IdEnti.SequenceEqual(request.IdEnti!) &&
                q.Mesi != null && q.Mesi.SequenceEqual(request.Mesi!) &&
                q.TipologiaContratto == request.TipologiaContratto &&
                q.TipologiaFattura == request.TipologiaFattura &&
                q.Azione == null &&
                q.Note == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/download ritorni NotFound
    /// quando il layer query ritorna null.
    /// </summary>
    public async Task PostPagoPAGestioneFattureDownloadAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFattureDownloadRequest { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureDownloadQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GestioneFattureListDto?)null);

        var result = await InvokePostGestioneFattureDownloadAsync(module, context, request, mediator.Object);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Does.Contain("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/download ritorni NotFound
    /// quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task PostPagoPAGestioneFattureDownloadAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new RicercaGestioneFattureDownloadRequest { Anno = 2026 };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureDownloadQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GestioneFattureListDto { GestioneFatture = Array.Empty<SimpleGestioneFattureDto>(), Count = 0 });

        var result = await InvokePostGestioneFattureDownloadAsync(module, context, request, mediator.Object);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetType().Name, Does.Contain("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/download propaghi
    /// la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void PostPagoPAGestioneFattureDownloadAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var request = new RicercaGestioneFattureDownloadRequest { Anno = 2026 };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureDownloadAsync(module, context, request, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura
    /// inoltri correttamente i filtri Anno/Mesi alla query e ritorni Ok.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneTipologiaFatturaAsync_ShouldMapFilters_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureTipologiaFatturaRequest
        {
            Anno = 2026,
            Mesi = new[] { 2, 3 }
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureTipologiaFatturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "ANTICIPO", "SECONDO SALDO" });

        var endpointResult = await InvokePostGestioneFattureTipologiaAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureTipologiaFatturaQuery>(q =>
                q.AuthenticationInfo != null &&
                q.Anno == request.Anno &&
                q.Mesi != null && q.Mesi.SequenceEqual(request.Mesi!)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura
    /// ritorni NotFound quando il layer query ritorna null.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneTipologiaFatturaAsync_WhenMediatorReturnsNull_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureTipologiaFatturaRequest { Anno = 2026, Mesi = new[] { 2 } };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureTipologiaFatturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)null);

        var endpointResult = await InvokePostGestioneFattureTipologiaAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura
    /// ritorni NotFound quando il layer query ritorna una lista vuota.
    /// </summary>
    public async Task GetPagoPAGestioneFatturazioneTipologiaFatturaAsync_WhenMediatorReturnsEmpty_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureTipologiaFatturaRequest { Anno = 2026, Mesi = new[] { 2 } };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureTipologiaFatturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        var endpointResult = await InvokePostGestioneFattureTipologiaAsync(module, context, request, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura
    /// propaghi la SecurityException quando il principal non è autenticato.
    /// </summary>
    public void GetPagoPAGestioneFatturazioneTipologiaFatturaAsync_WhenUserIsNotAuthenticated_ShouldThrowSecurityException()
    {
        var mediator = new Mock<IMediator>();
        var module = new FattureModule();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var request = new GestioneFattureTipologiaFatturaRequest { Anno = 2026, Mesi = new[] { 2 } };

        Assert.ThrowsAsync<SecurityException>(async () =>
            await InvokePostGestioneFattureTipologiaAsync(module, context, request, mediator.Object));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione
    /// mappi correttamente i campi della request e ritorni Ok quando il comando ha esito positivo.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_ShouldMapCommand_AndReturnOk()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "ELIMINA",
            Anno = 2026,
            Mese = 7,
            TipologiaFattura = "SECONDO SALDO",
            IdFattura = 123,
            Nota = new NoteCommand { Data = new DateTime(2026, 7, 1), Testo = "nota test" }
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureAzioneCommand>(q =>
                q.AuthenticationInfo != null &&
                q.AuthenticationInfo.Id == "user-test" &&
                q.IdUtente == "user-test" &&
                q.IdEnte == request.IdEnte &&
                q.Azione == request.Azione &&
                q.Anno == request.Anno &&
                q.Mese == request.Mese &&
                q.TipologiaFattura == request.TipologiaFattura &&
                q.IdFattura == request.IdFattura &&
                q.Nota != null && q.Nota.Testo == request.Nota!.Testo && q.Nota.Data == request.Nota.Data),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestCase("POSTICIPA")]
    [TestCase("ELIMINA")]
    [TestCase("RIPRISTINA")]
    [TestCase("CANCELLA")]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione
    /// inoltri tutte le azioni supportate dal contratto applicativo.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_ShouldForwardAllSupportedActions(string azione)
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = azione,
            Anno = 2026,
            Mese = 7,
            TipologiaFattura = "SECONDO SALDO",
            IdFattura = 123
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("Ok`1"));

        mediator.Verify(x => x.Send(
            It.Is<GestioneFattureAzioneCommand>(q => q.Azione == azione),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione ritorni NotFound
    /// quando il comando ritorna 0.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_WhenMediatorReturnsZero_ShouldReturnNotFound()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("NotFound"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione ritorni BadRequest
    /// quando il layer comando ritorna null.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_WhenMediatorReturnsNull_ShouldReturnBadRequest()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("BadRequest`1"));

        var value = innerResult.GetType().GetProperty("Value")?.GetValue(innerResult)?.ToString();
        Assert.That(value, Is.EqualTo("Risultato non valido."));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione ritorni BadRequest
    /// con il messaggio dell'ArgumentException quando il comando fallisce per azione non valida.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_WhenMediatorThrowsArgumentException_ShouldReturnBadRequestWithMessage()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "AZIONE_NON_VALIDA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("L'azione AZIONE_NON_VALIDA non esiste"));

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("BadRequest`1"));

        var value = innerResult.GetType().GetProperty("Value")?.GetValue(innerResult)?.ToString();
        Assert.That(value, Is.EqualTo("L'azione AZIONE_NON_VALIDA non esiste"));
    }

    [Test]
    /// <summary>
    /// Verifica che l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione ritorni il messaggio generico
    /// di BadRequest quando il comando lancia un'eccezione non gestita.
    /// </summary>
    public async Task PostPagoPAGestioneFattureAzioneAsync_WhenMediatorThrowsUnexpectedException_ShouldReturnGenericBadRequest()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<FattureModule>>();
        var module = new FattureModule();
        var context = BuildAuthenticatedHttpContext();
        var request = new GestioneFattureAzioneRequest
        {
            IdEnte = "11111111-1111-1111-1111-111111111111",
            Azione = "ELIMINA"
        };

        mediator
            .Setup(x => x.Send(It.IsAny<GestioneFattureAzioneCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var endpointResult = await InvokePostGestioneFattureAzioneAsync(module, context, request, logger.Object, mediator.Object);
        var innerResult = GetInnerResult(endpointResult);

        Assert.That(innerResult, Is.Not.Null);
        Assert.That(innerResult!.GetType().Name, Is.EqualTo("BadRequest`1"));

        var value = innerResult.GetType().GetProperty("Value")?.GetValue(innerResult)?.ToString();
        Assert.That(value, Is.EqualTo("Impossibile completare l'operazione."));
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
    /// Invoca l'endpoint GET /api/fatture/pagopa/gestione-fatture/anni tramite reflection.
    /// </summary>
    private static async Task<object> InvokeGetGestioneFattureAnniAsync(
        FattureModule module,
        HttpContext context,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "GetPagoPAGestioneFatturazioneAnniAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint anni non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint anni non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/mesi tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureMesiAsync(
        FattureModule module,
        HttpContext context,
        GestioneFattureMesiRequest request,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "PostPagoPAGestioneFatturazioneMesiAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint mesi non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint mesi non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/anni tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureModificaAnniAsync(
        FattureModule module,
        HttpContext context,
        GestioneFattureModificaRequest request,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "GetPagoPAModificaGestioneFatturazioneAnniAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint modifica anni non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint modifica anni non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/modifica/mesi tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureModificaMesiAsync(
        FattureModule module,
        HttpContext context,
        GestioneFattureModificaRequest request,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "GetPagoPAModificaGestioneFatturazioneMesiAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint modifica mesi non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint modifica mesi non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/tipologia-fattura tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureTipologiaAsync(
        FattureModule module,
        HttpContext context,
        GestioneFattureTipologiaFatturaRequest request,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "GetPagoPAGestioneFatturazioneTipologiaFatturaAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint tipologia non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint tipologia non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/download tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureDownloadAsync(
        FattureModule module,
        HttpContext context,
        RicercaGestioneFattureDownloadRequest request,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "PostPagoPAGestioneFattureDownloadAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint download non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint download non ha prodotto risultato.");
        return taskResult!;
    }

    /// <summary>
    /// Invoca l'endpoint POST /api/fatture/pagopa/gestione-fatture/azione tramite reflection.
    /// </summary>
    private static async Task<object> InvokePostGestioneFattureAzioneAsync(
        FattureModule module,
        HttpContext context,
        GestioneFattureAzioneRequest request,
        ILogger<FattureModule> logger,
        IMediator mediator)
    {
        var method = typeof(FattureModule).GetMethod(
            "PostPagoPAGestioneFattureAzioneAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, "Metodo endpoint azione non trovato via reflection.");

        var task = (Task)method!.Invoke(module, new object[] { context, request, logger, mediator })!;
        await task;

        var taskResult = task.GetType().GetProperty("Result")?.GetValue(task);
        Assert.That(taskResult, Is.Not.Null, "Il task dell'endpoint azione non ha prodotto risultato.");
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