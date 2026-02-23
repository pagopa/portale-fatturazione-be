using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ContestazioniRecapEnteOrchestrator
{
    [Function("ContestazioniRecapEnteOrchestrator")]
    public async Task<IEnumerable<ContestazioneRecap>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ContestazioniRecapEnteApiInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<ContestazioneRecap>>("ContestazioniRecapEnte", data); ;
    }
} 