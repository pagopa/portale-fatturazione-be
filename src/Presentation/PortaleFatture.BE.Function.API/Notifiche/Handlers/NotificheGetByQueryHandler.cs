using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Handlers;
internal class NotificheGetByQueryHandler
{
    [OpenApiOperation(operationId: "NotificheGetByQuery", tags: ["Notifiche"], Summary = "Ritorna le notifiche in base alla query richiesta.", Description = "Ritorna le notifiche in base alla query richiesta.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(NotificheRicercaRequestDocs), Description = "Request con parametri, i.e. anno e mese")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(NotificheResponse), Description = "Ritorna stato dell'operazione e link documento.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("NotificheGetByQueryHandler")]
    public async Task<HttpResponseData> NotificheGetByQueryOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/notifiche")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {

        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<NotificheRicercaRequest>();
        var data = request.Map(context.GetSession()); 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("NotificheGetByQueryOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Notifiche Get By Query Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}