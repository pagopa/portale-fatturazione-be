using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi.Orchestrators;

public class FattureGetAnniMesiOrchestrator
{
    [Function("FattureGetAnniMesiOrchestrator")]
    public async Task<IEnumerable<AnniMesiTipologiaByEnteDto>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELAnniMesiInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<AnniMesiTipologiaByEnteDto>>("FattureGetAnniMesi", data); ;
    }
}