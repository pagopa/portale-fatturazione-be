using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione;

public class RELTestataRicerca(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RELTestataRicerca>();

    [Function("RELTestataRicerca")]
    public async Task<RelTestataDto> RunAsync(
        [ActivityTrigger] RELTestataRicercaInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var request = req.Map(authInfo, req.Page, req.PageSize);

        var result = await mediator.Send(request);
        if (result == null || result.Count == 0)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.NotFound}{LoggerHelper.PIPE}{req.Session!.IdEnte.Serialize()}");
            throw new DomainException(MessageHelper.NotFound);
        } 

        await LogResponse(mediator, context, req, result);
        return result; 
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