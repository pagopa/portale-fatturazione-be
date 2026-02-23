using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Orchestrators;

public class NotificheGetAnniMesiOrchestrator
{
    [Function("NotificheGetAnniMesiOrchestrator")]
    public async Task<IEnumerable<NotificheAnniMesiResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<NotificheAnniMesiInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<NotificheAnniMesiResponse>>("NotificheGetAnniMesi", data); ;
    }
}