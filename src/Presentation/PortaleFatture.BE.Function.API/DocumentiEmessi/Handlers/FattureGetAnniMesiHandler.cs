using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.DocumentiEmessi.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi.Handlers;
 
public class FattureGetAnniMesiHandler
{
    [OpenApiOperation(operationId: "FattureGetAnniMesi", tags: ["Documenti Emessi"], Summary = "Permette di tornare i vari periodi storici con relativa tipologia di fattura.", Description = "Permette di tornare i vari periodi storici con relativa tipologia di fattura.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<AnniMesiTipologiaByEnteDto>), Description = "Torna i vari periodi storici con relativa tipologia di fattura.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("FattureGetAnniMesiHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/fatture/periodo")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    {
        var data = new FattureGetAnniMesiInternalRequest()
        {
            Session = context.GetSession(),
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("FattureGetAnniMesiOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Fatture Get Anni Mesi Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}