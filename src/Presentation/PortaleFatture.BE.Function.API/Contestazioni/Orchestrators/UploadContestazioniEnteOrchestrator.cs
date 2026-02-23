using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;


public class UploadContestazioniEnteOrchestrator
{
    [Function("UploadContestazioniEnteOrchestrator")]
    public async Task<string?> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<UploadContestazioniEnteApiInternalRequest>();
        return await context.CallActivityAsync<string?>("UploadContestazioniEnte", data); ;
    }
}