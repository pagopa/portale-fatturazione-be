using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class ModuloCommessaGetByAnnoMeseDettaglioHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaGetByAnnoMeseDettaglioHandler", tags: ["Modulo Commessa"], Summary = "Permette di tornare il dettaglio modulo commessa per anno e mese.", Description = "Permette di tornare il dettaglio modulo commessa per anno e mese.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ModuloCommessaGetByAnnoMeseDettaglioRequest), Description = "Modulo Commessa Dettaglio request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ModuloCommessaDocumentoDto), Description = "Modulo Commessa per Anno e Mese")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("ModuloCommessaGetByAnnoMeseDettaglioHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetByAnnoMeseDettaglioOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post",  Route = "v1/modulocommessa/dettaglio")] HttpRequestData req,
    [DurableClient] DurableTaskClient client, 
     FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ModuloCommessaGetByAnnoMeseDettaglioRequest>();
        var data = new ModuloCommessaGetByAnnoMeseDettaglioInternalRequest()
        {
            Anno = request.Anno,
            Mese = request.Mese,
            Session = context.GetSession() 
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaGetByAnnoMeseDettaglioOrchestrator", data); 
        var originalPayload = client.CreateHttpManagementPayload(instanceId); 
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!); 
        var payload = new
        {
            message = "Modulo Commessa detagglio Anno Mese Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}