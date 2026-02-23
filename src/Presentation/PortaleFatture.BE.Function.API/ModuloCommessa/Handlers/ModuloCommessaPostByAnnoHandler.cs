using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class ModuloCommessaPostByAnnoHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaGetByAnnoHandler", tags: ["Modulo Commessa"], Summary = "Permette di tornare un sommario di tutti i moduli commessa inseriti per anno.", Description = "Permette di tornare un sommario di tutti i moduli commessa inseriti per anno.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ModuloCommessaGetByAnnoRequest), Description = "Modulo Commessa request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<DatiModuloCommessaByAnnoResponse>), Description = "Modulo Commessa per Anno")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("ModuloCommessaGetByAnnoHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post",  Route = "v1/modulocommessa/anno")] HttpRequestData req,
    [DurableClient] DurableTaskClient client, 
     FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ModuloCommessaGetByAnnoRequest>();
        var data = new ModuloCommessaGetByAnnoInternalRequest()
        {
            Anno = request.Anno,
            Session = context.GetSession() 
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaGetByAnnoOrchestrator", data); 
        var originalPayload = client.CreateHttpManagementPayload(instanceId); 
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!); 
        var payload = new
        {
            message = "Modulo Commessa per Anno Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}