using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaGetByAnnoOrchestrator
{
    [Function("ModuloCommessaGetByAnnoOrchestrator")]
    public async Task<IEnumerable<DatiModuloCommessaByAnnoResponse>> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoInternalRequest>();
        return await context.CallActivityAsync<IEnumerable<DatiModuloCommessaByAnnoResponse>>("ModuloCommessaGetByAnnoData", data);
    }
}