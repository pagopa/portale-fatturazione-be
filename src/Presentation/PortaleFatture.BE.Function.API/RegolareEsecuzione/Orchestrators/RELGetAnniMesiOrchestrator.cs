using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Orchestrators;

public class RELGetAnniMesiOrchestrator
{
    [Function("RELGetAnniMesiOrchestrator")]
    public async Task<IEnumerable<RELAnniMesiResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELAnniMesiInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<RELAnniMesiResponse>>("RELGetAnniMesi", data); ;
    }
}