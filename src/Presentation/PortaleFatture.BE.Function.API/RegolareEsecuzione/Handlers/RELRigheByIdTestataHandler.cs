using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Handlers;

public class RELRigheByIdTestataHandler
{
    [OpenApiOperation(operationId: "RELRigheByIdTestataHandler", tags: ["Regolare Esecuzione"], Summary = "Ritorna righe regolare esecuzione.", Description = "Ritorna righe regolare esecuzione.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RELRigheByIdTestataResponse), Description = "Ritorna SAS token per scaricare le righe REL.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RELRigheByIdTestataRequest), Description = "Richiesta con id testata.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("RELRigheByIdTestataHandler")]
    public async Task<HttpResponseData> RELRigheByIdTestataOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/rel/righe")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<RELRigheByIdTestataRequest>();
        var data = new RELRigheByIdTestataInternalRequest()
        {
            IdTestata = request.IdTestata,
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("RELRigheByIdTestataOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "REL RigheBy Id Testata Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}

