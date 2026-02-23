using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Notifiche.Handlers;
internal class NotificheGetFlagContestazioneHandler
{
    [OpenApiOperation(operationId: "NotificheGetFlagContestazione", tags: ["Notifiche"], Summary = "Ritorna flag contestazioni.", Description = "Ritorna flag contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<FlagContestazione>), Description = "Ritorna anni e mesi relativi a notifiche.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("NotificheGetFlagContestazioneHandler")]
    public async Task<HttpResponseData> NotificheGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/notifiche/flag")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    { 
        var data = new NotificheFlagContestazioneInternalRequest()
        {
            Session = context.GetSession()
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("NotificheGetFlagContestazioneOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Notifiche Get Flag Contestazione Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}