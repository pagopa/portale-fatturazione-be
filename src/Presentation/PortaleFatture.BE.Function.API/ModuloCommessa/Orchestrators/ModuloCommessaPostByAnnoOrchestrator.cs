using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaPostByAnnoOrchestrator
{
    [Function("ModuloCommessaGetByAnnoOrchestrator")]
    public async Task<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>>("ModuloCommessaPostByAnnoData", data);
    }
}