using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

namespace PortaleFatture.BE.Function.API.Contestazioni;

public class TipoContestazioni(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<TipoReport>();

    [Function("TipoContestazioni")]
    public async Task<IEnumerable<TipoContestazione>?> RunAsync(
        [ActivityTrigger] TipoContestazioniInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");
 

        var tipologia = await mediator.Send(new TipoContestazioneGetAll());
        if (tipologia.IsNullNotAny())
            throw new DomainException($"Non ci sono tipologie contestazioni registrate. {req.Session!.IdEnte!}");

        await LogResponse(mediator, context, req, tipologia!);
        return tipologia;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, TipoContestazioniInternalRequest request, IEnumerable<TipoContestazione> response)
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