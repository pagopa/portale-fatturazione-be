using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators; 
 
public class ContestazioniAzioneGetAnniMesiOrchestrator
{
    [Function("ContestazioniAzioneGetAnniMesiOrchestrator")]
    public async Task<IEnumerable<NotificheAnniMesiResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<NotificheAnniMesiInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<NotificheAnniMesiResponse>>("ContestazioniAzioneGetAnniMesi", data); ;
    }
}