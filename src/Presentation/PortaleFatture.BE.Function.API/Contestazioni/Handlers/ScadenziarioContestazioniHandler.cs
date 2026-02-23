using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;
 
internal class ScadenziarioContestazioniHandler
{
    [OpenApiOperation(operationId: "ScadenzarioContestazioniHandler", tags: ["Contestazioni"], Summary = "Ritorna scadenzario contestazioni.", Description = "Ritorna scadenzario contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<CalendarioContestazioniExtendedResponse>), Description = "Ritorna scadenzario contestazioni.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ScadenzarioContestazioniHandler")]
    public async Task<HttpResponseData> StepsContestazioniEnteOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/contestazioni/scadenzario")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var data = new ScadenziarioContestazioniInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ScadenzarioContestazioniOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Scadenzario Contestazioni Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}