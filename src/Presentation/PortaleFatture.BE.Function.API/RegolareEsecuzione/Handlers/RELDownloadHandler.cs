using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Handlers;

public class RELDownloadHandler
{
    [OpenApiOperation(operationId: "RELDownloadHandler", tags: ["Regolare Esecuzione"], Summary = "Ritorna il documento REL in formato PDF o HTML.", Description = "Ritorna il documento REL in formato PDF o HTML.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RELDownloadRequest), Description = "Download documento request")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, "application/json", typeof(RELDownloadResponse), Description = "SAS token che punta al documento REL in formato PDF o HTML. Nel primo caso 'application/pdf' altrimenti 'text/html'")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("RELDownloadHandler")]
    public async Task<HttpResponseData> RELDownloadOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/rel/download")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<RELDownloadRequest>();
        var data = request.Map(context.GetSession());

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("RelDownloadOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Rel Donwload Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}