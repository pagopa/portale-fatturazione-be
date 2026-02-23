using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Orchestrators;

public class RELTestataRicercaOrchestrator
{
    [Function("RELTestataRicercaOrchestrator")]
    public async Task<RelTestataDto> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<RELTestataRicercaInternalRequest>();
        return await context.CallActivityAsync<RelTestataDto>("RELTestataRicerca", data);  
    }
}