using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ContestazioniReportEnteOrchestrator
{
    [Function("ContestazioniReportEnteOrchestrator")]
    public async Task<ReportContestazioniList> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ContestazioniReportEntePagingInternalRequest>();
        return await context.CallActivityAsync<ReportContestazioniList>("ContestazioniReportEnte", data); ;
    }
}