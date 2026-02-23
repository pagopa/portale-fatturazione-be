using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Orchestrators;

public class NotificheGetByQueryOrchestrator
{
    [Function("NotificheGetByQueryOrchestrator")]
    public async Task<NotificheResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<NotificheRicercaInternalRequest>();
        return await context.CallActivityAsync<NotificheResponse>("NotificheGetByQuery", data); ;
    }
}