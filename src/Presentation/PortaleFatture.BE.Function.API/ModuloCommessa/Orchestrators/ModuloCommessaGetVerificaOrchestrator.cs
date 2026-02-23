using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators; 

public class ModuloCommessaGetVerificaOrchestrator
{
    [Function("ModuloCommessaGetVerificaOrchestrator")]
    public async Task<bool> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ModuloCommessaGetVerificaInternalRequest>();
        return await context.CallActivityAsync<bool>("ModuloCommessaGetVerifica", data);
    }
}