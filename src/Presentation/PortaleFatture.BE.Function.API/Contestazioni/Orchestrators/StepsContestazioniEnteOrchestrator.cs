using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class StepsContestazioniEnteOrchestrator
{
    [Function("StepsContestazioniEnteOrchestrator")]
    public async Task<IEnumerable<ContestazioneStep>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<StepsContestazioniEnteInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<ContestazioneStep>>("StepsContestazioni", data); ;
    }
}