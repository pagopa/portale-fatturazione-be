using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Orchestrators;

public class RELRigheByIdTestataOrchestrator
{
    [Function("RELRigheByIdTestataOrchestrator")]
    public async Task<RELRigheByIdTestataResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELRigheByIdTestataInternalRequest>();
        return await context.CallActivityAsync<RELRigheByIdTestataResponse>("RELRigheByIdTestata", data);
    }
}