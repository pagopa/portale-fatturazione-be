using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;
 
internal class ContestazioniGetAnniMesiHandler
{
    [OpenApiOperation(operationId: "ContestazioniGetAnniMesi", tags: ["Contestazioni"], Summary = "Ritorna anni e mesi archivio relativi alle contestazioni.", Description = "Ritorna anni e mesi relativi a contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<NotificheAnniMesiResponse>), Description = "Ritorna anni e mesi archivio relativi a contestazioni.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniGetAnniMesiHandler")]
    public async Task<HttpResponseData> ContestazioniGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/contestazioni/archivio/periodo")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var data = new NotificheAnniMesiInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniGetAnniMesiOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Archivio Get Anni Mesi Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}