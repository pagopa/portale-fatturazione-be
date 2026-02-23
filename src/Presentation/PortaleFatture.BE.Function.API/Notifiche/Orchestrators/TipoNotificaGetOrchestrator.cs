using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Orchestrators;

public class TipoNotificaGetOrchestrator
{
    [Function("TipoNotificaGetOrchestrator")]
    public async Task<IEnumerable<TipoNotificaResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<TipoNotificaInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<TipoNotificaResponse>>("TipoNotificaGet", data); ;
    }
}