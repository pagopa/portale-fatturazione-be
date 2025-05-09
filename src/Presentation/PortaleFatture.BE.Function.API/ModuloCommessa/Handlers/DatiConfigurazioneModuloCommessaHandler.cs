using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.DatiConfigurazioneModuloCommesse.Response;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class DatiConfigurazioneModuloCommessaHandler
{
    [OpenApiOperation(operationId: "DatiConfigurazioneModuloCommessa", tags: ["Modulo Commessa"], Summary = "Ritorna la configurazione per inserire il modulo commessa. In particolare il campo: idTipoSpedizione", Description = "Ritorna la configurazione per inserire il modulo commessa.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DatiConfigurazioneModuloCommessaResponse), Description = "Configurazione Modulo Commessa")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("DatiConfigurazioneModuloCommessaHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get",  Route = "v1/modulocommessa/configurazione")] HttpRequestData req,
    [DurableClient] DurableTaskClient client, 
     FunctionContext context)
    { 
        var data = new DatiConfigurazioneModuloCommessaRequest()
        {
            Session = context.GetSession(),
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("DatiConfigurazioneModuloCommessaOrchestrator", data); 
        var originalPayload = client.CreateHttpManagementPayload(instanceId); 
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!); 
        var payload = new
        {
            message = "Dati Configurazione Modulo Commessa Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}