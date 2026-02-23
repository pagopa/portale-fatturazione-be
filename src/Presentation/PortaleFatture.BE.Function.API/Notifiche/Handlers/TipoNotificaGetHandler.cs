using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Handlers;
internal class TipoNotificaGetHandler
{
    [OpenApiOperation(operationId: "NotificheGetAnniMesi", tags: ["Notifiche"], Summary = "Ritorna le tipologie notifiche.", Description = "Ritorna le tipologie notifiche.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<TipoNotificaResponse>), Description = "Ritorna le tipologie notifiche.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("TipoNotificaGetHandler")]
    public async Task<HttpResponseData> NotificheGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/notifiche/tipologia")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    { 
        var data = new TipoNotificaInternalRequest()
        {
            Session = context.GetSession()
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("TipoNotificaGetOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Tipo Notifica Get Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}