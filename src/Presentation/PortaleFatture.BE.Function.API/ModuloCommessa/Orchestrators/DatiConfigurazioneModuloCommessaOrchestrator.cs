using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Response;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class DatiConfigurazioneModuloCommessaOrchestrator
{
    [Function("DatiConfigurazioneModuloCommessaOrchestrator")]
    public async Task<DatiConfigurazioneModuloCommessaResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoInternalRequest>();
        return await context.CallActivityAsync<DatiConfigurazioneModuloCommessaResponse>("DatiConfigurazioneModuloCommessa", data);
    }
}