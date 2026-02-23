using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaGetAnniMesiOrchestrator
{
    [Function("ModuloCommessaGetAnniMesiOrchestrator")]
    public async Task<IEnumerable<string>?> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<string>?>("ModuloCommessaGetAnniMesiData", data);
    }
}