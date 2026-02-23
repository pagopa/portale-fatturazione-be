using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Handlers;

public class RELTestataRicercaHandler
{
    [OpenApiOperation(operationId: "RELTestataRicercaHandler", tags: ["Regolare Esecuzione"], Summary = "Ritorna la testata della REL per query.", Description = "Ritorna la testata della REL per query.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RelTestataDto), Description = "Ritorna la testata della REL per query.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RELTestataRicercaExternalRequest), Description = "Ritorna la testata della REL per request anno e mese e paging")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("RELTestataRicercaHandler")]
    public async Task<HttpResponseData> NotificheGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/rel/testata")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<RELTestataRicercaExternalRequest>();
        var data = request.Map(context.GetSession());

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("RELTestataRicercaOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "REL Testata Ricerca Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}