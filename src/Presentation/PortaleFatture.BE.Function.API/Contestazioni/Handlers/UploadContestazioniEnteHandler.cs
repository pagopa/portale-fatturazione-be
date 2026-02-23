using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers; 
 
internal class UploadContestazioniEnteHandler
{
    [OpenApiOperation(operationId: "UploadContestazioniEnteHandler", tags: ["Contestazioni"], Summary = "Ritorna SAS token per upload file.", Description = "Ritorna SAS token per upload file.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UploadContestazioniEnteApiRequest), Description = "Request con parametri anno e mese.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "SAS token per upload file contestazioni.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("UploadContestazioniEnteHandler")]
    public async Task<HttpResponseData> UploadContestazioniEnteOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/upload")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<UploadContestazioniEnteApiRequest>();
        var data = new UploadContestazioniEnteApiInternalRequest()
        {
            Session = context.GetSession(),
            Anno = request.Anno, 
            Mese = request.Mese, 
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("UploadContestazioniEnteOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Upload Contestazioni Ente Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}