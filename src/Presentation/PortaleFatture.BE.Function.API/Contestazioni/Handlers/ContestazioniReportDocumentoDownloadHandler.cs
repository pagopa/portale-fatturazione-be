using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;


internal class ContestazioniReportDocumentoDownloadHandler
{
    [OpenApiOperation(operationId: "ContestazioniReportDocumentoDownloadHandler", tags: ["Contestazioni"], Summary = "Ritorna documento contestazione in formato csv/json", Description = "Ritorna documento contestazione in formato csv/json")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ContestazioniReportDocumentoDownloadRequest), Description = "Request con id report e tiporeport, valori ammissibile: csv e json")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ContestazioniReportDocumentoDownloadResponse), Description = "Ritorna il sas token del documento richiesto in base al tipo report.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniReportDocumentoDownloadHandler")]
    public async Task<HttpResponseData> CheckDownloadNotificheOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/report/download")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ContestazioniReportDocumentoDownloadRequest>();
        var data = new ContestazioniReportDocumentoDownloadInternalRequest()
        {
            Session = context.GetSession(),
            IdReport = request!.IdReport,
            TipoReport = request.TipoReport
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniReportDocumentoDownloadOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Report Documento Download Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}