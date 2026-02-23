using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers; 

internal class ReportContestazioneStepsHandler
{
    [OpenApiOperation(operationId: "ReportContestazioneStepsHandler", tags: ["Contestazioni"], Summary = "Ritorna step relativi a un singolo report contestazioni.", Description = "Ritorna step relativi a un singolo report contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<ReportContestazioneStepsDto>), Description = "Ritorna step relativi a un singolo report contestazioni.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ReportContestazioneStepsRequest), Description = "Request con id report.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ReportContestazioneStepsHandler")]
    public async Task<HttpResponseData> ReportContestazioneStepsOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/reports/steps")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ReportContestazioneStepsRequest>();
        var data = new ReportContestazioneStepsInternalRequest()
        {
            Session = context.GetSession(), 
             IdReport = request.IdReport
        }; 

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ReportContestazioneStepsOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Report Contestazione Steps Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}