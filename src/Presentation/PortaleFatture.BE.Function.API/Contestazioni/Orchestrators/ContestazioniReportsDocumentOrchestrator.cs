using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ContestazioniReportsDocumentOrchestrator
{
    [Function("ContestazioniReportsDocumentOrchestrator")]
    public async Task<string> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ContestazioniReportsDocumentInternalRequest>();
        return await context.CallActivityAsync<string>("ContestazioniReportsDocument", data); ;
    }
}