using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;
 
internal class TipoContestazioneHandler
{
    [OpenApiOperation(operationId: "TipoContestazioneHandler", tags: ["Contestazioni"], Summary = "Ritorna tipologia contestazioni.", Description = "Ritorna tipologia contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<TipoContestazione>), Description = "Ritorna tipologia contestazioni.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("TipoContestazioneHandler")]
    public async Task<HttpResponseData> StepsContestazioniEnteOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/contestazioni/tipologia")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var data = new TipoContestazioniInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("TipoContestazioneOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Tipo Contestazione Ente Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}