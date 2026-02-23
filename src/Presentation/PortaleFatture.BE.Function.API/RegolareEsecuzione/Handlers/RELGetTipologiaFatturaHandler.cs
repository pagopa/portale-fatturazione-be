using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Handlers;

public class RELGetTipologiaFatturaHandler
{
    [OpenApiOperation(operationId: "RELGetTipologiaFatturaHandler", tags: ["Regolare Esecuzione"], Summary = "Ritorna tipologia fattura relative alla regolare esecuzione.", Description = "Ritorna tipologia fattura relative alla regolare esecuzione.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<string>), Description = "Ritorna tipologia fattura relative alla regolare esecuzione.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RelTipologiaFatturaRequest), Description = "Ricerca tipologia fattura per anno e mese")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("RELGetTipologiaFatturaHandler")]
    public async Task<HttpResponseData> RELGetTipologiaFatturaOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/rel/tipologiafattura")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<RelTipologiaFatturaRequest>();
        var data = new RELTipologiaFatturaInternalRequest()
        {
            Anno = request.Anno,
            Mese = request.Mese,
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("RelTipologiaFatturaOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "REL Get Tipologia Fattura Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}
