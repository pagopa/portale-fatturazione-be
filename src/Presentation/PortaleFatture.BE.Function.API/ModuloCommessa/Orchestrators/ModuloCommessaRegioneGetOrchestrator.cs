using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaRegioneGetOrchestrator
{
    [Function("ModuloCommessaRegioneGetOrchestrator")]
    public async Task<IEnumerable<ModuloCommessaRegioneDto>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaRegioneInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<ModuloCommessaRegioneDto>>("ModuloCommessaRegioneGet", data);
    }
}