using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers;

internal class ContestazioniReportEnteHandler
{
    [OpenApiOperation(operationId: "ContestazioniReportEnteHandler", tags: ["Contestazioni"], Summary = "Ritorna report contestazioni per tipologia.", Description = "Ritorna report contestazioni per tipologia.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ContestazioniReportEntePagingRequest), Description = "Request con parametri, i.e. tipologia report, e paging.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ReportContestazioniList), Description = "Ritorna report contestazioni per tipologia.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniReportEnteHandler")]
    public async Task<HttpResponseData> ContestazioniReportEnteOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/reports")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ContestazioniReportEntePagingRequest>();
        var data = new ContestazioniReportEntePagingInternalRequest()
        {
            Session = context.GetSession(),
            Anno = request.Anno,
            IdTipologiaReports = request.IdTipologiaReports,
            Mese = request.Mese,
            Page = request.Page,
            PageSize = request.PageSize 
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniReportEnteOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Report Ente Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}