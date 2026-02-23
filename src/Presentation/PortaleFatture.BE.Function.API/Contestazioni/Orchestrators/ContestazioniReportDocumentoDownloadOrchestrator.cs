using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ContestazioniReportDocumentoDownloadOrchestrator
{
    [Function("ContestazioniReportDocumentoDownloadOrchestrator")]
    public async Task<ContestazioniReportDocumentoDownloadResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ContestazioniReportDocumentoDownloadInternalRequest>();
        return await context.CallActivityAsync<ContestazioniReportDocumentoDownloadResponse>("ContestazioniReportDocumentoDownload", data); ;
    }
}