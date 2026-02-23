using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Handlers;
internal class NotificheGetAnniMesiHandler
{
    [OpenApiOperation(operationId: "NotificheGetAnniMesi", tags: ["Notifiche"], Summary = "Ritorna anni e mesi relativi a notifiche.", Description = "Ritorna anni e mesi relativi a notifiche.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<NotificheAnniMesiResponse>), Description = "Ritorna anni e mesi relativi a notifiche.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("NotificheGetAnniMesiHandler")]
    public async Task<HttpResponseData> NotificheGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/notifiche/periodo")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    { 
        var data = new NotificheAnniMesiInternalRequest()
        {
            Session = context.GetSession()
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("NotificheGetAnniMesiOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Notifiche Get Anni Mesi Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}