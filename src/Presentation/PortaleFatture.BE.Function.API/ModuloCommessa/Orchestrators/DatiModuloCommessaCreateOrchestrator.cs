using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class DatiModuloCommessaCreateOrchestrator
{
    [Function("DatiModuloCommessaCreateOrchestrator")]
    public async Task<DatiModuloCommessaResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<DatiModuloCommessaCreateInternalRequest>();
        return await context.CallActivityAsync<DatiModuloCommessaResponse>("DatiModuloCommessaCreate", data);
    }
}