using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche;

public class TipoNotificaGet(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<NotificheGetAnniMesi>();

    [Function("TipoNotificaGet")]
    public async Task<IEnumerable<TipoNotificaResponse>?> RunAsync(
        [ActivityTrigger] TipoNotificaInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");


        var result = NotificheExtensions.GetAllTipologiaNotifica();
        await LogResponse(mediator, context, req, result);
        return result; 
    }


    private async Task LogResponse(IMediator mediator, FunctionContext context, TipoNotificaInternalRequest request, IEnumerable<TipoNotificaResponse> response)
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