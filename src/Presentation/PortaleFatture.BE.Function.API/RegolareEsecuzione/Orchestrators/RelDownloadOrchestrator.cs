using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Orchestrators; 
public class RelDownloadOrchestrator
{
    [Function("RelDownloadOrchestrator")]
    public async Task<RELDownloadResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELDownloadInternalRequest>();
        return await context.CallActivityAsync<RELDownloadResponse>("RelDownload", data);
    }
}