using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Orchestrators;
 
public class ScadenzarioContestazioniOrchestrator
{
    [Function("ScadenzarioContestazioniOrchestrator")]
    public async Task<IEnumerable<CalendarioContestazioniExtendedResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ScadenziarioContestazioniInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<CalendarioContestazioniExtendedResponse>>("ScadenzarioContestazioni", data); ;
    }
}