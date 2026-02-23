using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.DocumentiEmessi.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi.Orchestrators;
 
public class FattureEnteByRicercaOrchestrator
{
    [Function("FattureEnteByRicercaOrchestrator")]
    public async Task<FattureListaDto> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<FatturaRicercaEnteInternalRequest>();
        return await context.CallActivityAsync<FattureListaDto>("FatturaRicercaEnte", data); ;
    }
}