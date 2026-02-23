using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;

public class TipoContestazioneOrchestrator
{
    [Function("TipoContestazioneOrchestrator")]
    public async Task<IEnumerable<TipoContestazione>?> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<TipoContestazioniInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<TipoContestazione>?>("TipoContestazioni", data); ;
    }
}