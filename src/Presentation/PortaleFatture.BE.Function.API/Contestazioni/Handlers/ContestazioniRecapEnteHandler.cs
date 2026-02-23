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


internal class ContestazioniRecapEnteHandler
{
    [OpenApiOperation(operationId: "ContestazioniRecapEnteHandler", tags: ["Contestazioni"], Summary = "Ritorna recap relativo a contestazioni.", Description = "Ritorna recap relativo a contestazioni.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<ContestazioneRecap>), Description = "Ritorna recap relativo a contestazioni.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ContestazioniRecapEnteApiRequest), Description = "Request con anno e mese")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ContestazioniRecapEnteHandler")]
    public async Task<HttpResponseData> ReportContestazioneStepsOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/recap")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<ContestazioniRecapEnteApiRequest>();
        var data = new ContestazioniRecapEnteApiInternalRequest()
        {
            Session = context.GetSession(),
            Anno = request.Anno!.Value,
            Mese = request.Mese!.Value
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ContestazioniRecapEnteOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Contestazioni Recap Ente Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}