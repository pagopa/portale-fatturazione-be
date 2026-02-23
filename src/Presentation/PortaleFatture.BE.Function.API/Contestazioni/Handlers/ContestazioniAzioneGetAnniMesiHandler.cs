using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;
 
internal class ContestazioniAzioneGetAnniMesiHandler
{
    [OpenApiOperation(operationId: "ContestazioniAzioneGetAnniMesiHandler", tags: ["Contestazioni"], Summary = "Ritorna anni e mesi azione relativi alle contestazioni.", Description = "Ritorna anni e mesi azione relativi a contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<NotificheAnniMesiResponse>), Description = "Ritorna anni e mesi azione relativi a contestazioni.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniAzioneGetAnniMesiHandler")]
    public async Task<HttpResponseData> ContestazioniAzioneGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/contestazioni/azione/periodo")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var data = new NotificheAnniMesiInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniAzioneGetAnniMesiOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Azione Get Anni Mesi Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}