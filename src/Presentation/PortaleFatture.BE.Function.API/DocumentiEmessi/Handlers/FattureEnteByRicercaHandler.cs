using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.DocumentiEmessi.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi.Handlers;

public class FattureEnteByRicercaHandler
{
    [OpenApiOperation(operationId: "FattureEnteByRicerca", tags: ["Documenti Emessi"], Summary = "Permette di tornare le fatture per periodo e tipologia.", Description = "Permette di tornare le fatture per periodo e tipologia.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FatturaRicercaEnteRequest), Description = "Fatture request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<FatturaDto>), Description = "Torna le fatture per periodo e tipologia.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("FattureEnteByRicercaHandler")]
    public async Task<HttpResponseData?> FattureEnteByRicercaOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/fatture/ricerca")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<FatturaRicercaEnteRequest>();

        var data = new FatturaRicercaEnteInternalRequest()
        {
            Session = context.GetSession(),
            Anno = request.Anno,
            Mese = request.Mese,
            TipologiaFattura = request.TipologiaFattura,
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("FattureEnteByRicercaOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Fatture Ente By Ricerca Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}