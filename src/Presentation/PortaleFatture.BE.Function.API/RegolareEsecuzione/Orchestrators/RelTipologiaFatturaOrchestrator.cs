using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Orchestrators; 
public class RelTipologiaFatturaOrchestrator
{
    [Function("RelTipologiaFatturaOrchestrator")]
    public async Task<IEnumerable<string>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELTipologiaFatturaInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<string>>("RelTipologiaFattura", data);
    }
}