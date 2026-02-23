using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;
internal class ContestazioniReportsDocumentHandler
{
    [OpenApiOperation(operationId: "ContestazioniReportsDocumentHandler", tags: ["Contestazioni"], Summary = "Ritorna sas token documento singlo step and report.", Description = "Ritorna sas token documento singlo step and report.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ContestazioniDocumentRequest), Description = "Request con parametri, i.e. id report e id step")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Ritorna sas token documento singlo step and report.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniReportsDocumentHandler")]
    public async Task<HttpResponseData> ContestazioniReportsDocumentOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/reports/document")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ContestazioniDocumentRequest>();
        var data = new ContestazioniReportsDocumentInternalRequest()
        {
            Session = context.GetSession(),
            IdReport = request.IdReport,
            Step = request.Step,
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniReportsDocumentOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Reports Document Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}