using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;

public class TipoReportOrchestrator
{
    [Function("TipoReportOrchestrator")]
    public async Task<IEnumerable<TipologiaReport>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<TipoReportInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<TipologiaReport>>("TipoReport", data); ;
    }
}