using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Orchestrators;

public class ModuloCommessaGetByAnnoMeseDettaglioOrchestrator
{
    [Function("ModuloCommessaGetByAnnoMeseDettaglioOrchestrator")]
    public async Task<ModuloCommessaDocumentoDto> RunAsync(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    { 
        var data = context.GetInput<ModuloCommessaGetByAnnoMeseDettaglioInternalRequest>();
        return await context.CallActivityAsync<ModuloCommessaDocumentoDto>("ModuloCommessaGetByAnnoMeseDettaglio", data);
    }
}