using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

namespace PortaleFatture.BE.Function.API.Notifiche;

public class NotificheGetFlagContestazione(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<NotificheGetFlagContestazione>();

    [Function("NotificheGetFlagContestazione")]
    public async Task<IEnumerable<FlagContestazione>?> RunAsync(
        [ActivityTrigger] NotificheFlagContestazioneInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

 
        var flags = await mediator.Send(new FlagContestazioneQueryGetAll());
        if (flags.IsNullNotAny())
            throw new DomainException($"Non ci sono flag contestazioni registrate. {req.Session!.IdEnte!}");  

        await LogResponse(mediator, context, req, flags);
        return flags; 
    }

    private async Task LogResponse(IMediator mediator, 
        FunctionContext context, 
        NotificheFlagContestazioneInternalRequest request, 
        IEnumerable<FlagContestazione> response)
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