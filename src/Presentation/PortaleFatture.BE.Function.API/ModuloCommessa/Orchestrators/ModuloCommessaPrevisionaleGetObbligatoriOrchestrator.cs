using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaPrevisionaleGetObbligatoriOrchestrator
{
    [Function("ModuloCommessaPrevisionaleGetObbligatoriOrchestrator")]
    public async Task<ModuloCommessaPrevisionaleObbligatoriResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var data = context.GetInput<ModuloCommessaPrevisionaleObbligatoriInternalRequest>();
        return await context.CallActivityAsync<ModuloCommessaPrevisionaleObbligatoriResponse>("ModuloCommessaPrevisionaleGetObbligatori", data);
    }
}