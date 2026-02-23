using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaGetByAnnoMeseDettaglioOrchestrator
{
    [Function("ModuloCommessaGetByAnnoMeseDettaglioOrchestrator")]
    public async Task<ModuloCommessaPrevisionaleObbligatoriResponse> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoMeseDettaglioInternalRequest>();
        return await context.CallActivityAsync<ModuloCommessaPrevisionaleObbligatoriResponse>("ModuloCommessaGetByAnnoMeseDettaglio", data);
    }
}