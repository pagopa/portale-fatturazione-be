using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione;

public class RELRigheByIdTestata(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RELRigheByIdTestata>();

    [Function("RELRigheByIdTestata")]
    public async Task<RELRigheByIdTestataResponse> RunAsync(
        [ActivityTrigger] RELRigheByIdTestataInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var storageService = context.InstanceServices.GetRequiredService<IRelRigheStorageService>();

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var idEnte = req.IdTestata!.Split("_")[0];
        if (idEnte != authInfo.IdEnte)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.NotFound}{LoggerHelper.PIPE}{req.Session!.IdEnte.Serialize()}");
            throw new DomainException(MessageHelper.NotFound);
        }

        var ente = await mediator.Send(new EnteQueryGetById(authInfo) { });
        if (ente == null)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.NotFound}{LoggerHelper.PIPE}{req.Session!.IdEnte.Serialize()}");
            throw new DomainException(MessageHelper.NotFound);
        }

        var url = storageService.GetSASToken(req.IdTestata, ente.Descrizione!);
        url = url!.FileExistsAsync();
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.NotFound}{LoggerHelper.PIPE}{req.Session!.IdEnte.Serialize()}");
            throw new DomainException(MessageHelper.NotFound);
        }

        return new RELRigheByIdTestataResponse()
        {
            LinkDocumento = url,
        };
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, RELTestataRicercaInternalRequest request, RelTestataDto response)
    {
        var sse = request.Session;
        sse!.Payload = response.Serialize();
        var logResponse = context.Response(sse);
        try
        {
            await mediator.Send(logResponse);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.BadRequestLogging}{LoggerHelper.PIPE}{ex.Serialize()}");
            throw new DomainException(MessageHelper.BadRequestLogging, ex);
        }
    }
}
