using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ReportContestazioneStepsOrchestrator
{
    [Function("ReportContestazioneStepsOrchestrator")]
    public async Task<IEnumerable<ReportContestazioneStepsDto>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ReportContestazioneStepsInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<ReportContestazioneStepsDto>>("ReportContestazioneSteps", data); ;
    }
}