using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers; 

public class DatiModuloCommessaCreateHandler
{
    [OpenApiOperation(operationId: "DatiModuloCommessaCreateInternalHandler", tags: ["Modulo Commessa"], Summary = "Permette di inserire o modificare il modulo commessa nel periodo di riferimento [1,19] mese corrente", Description = "Permette di inserire o modificare il modulo commessa nel periodo di riferimento [1,19] mese corrente per il mese sussessivo (mese + 1)")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(DatiModuloCommessaCreateRequest), Description = "Modulo Commessa request insert or update")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DatiModuloCommessaResponse), Description = "Modulo Commessa inserita per periodo di riferimento.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("DatiModuloCommessaCreateInternalHandler")]
    public async Task<HttpResponseData?> DatiModuloCommessaCreateOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/modulocommessa")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<DatiModuloCommessaCreateRequest>();
        var data = new DatiModuloCommessaCreateInternalRequest()
        {
            DatiModuloCommessaCreate = request,
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("DatiModuloCommessaCreateOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Inserimento o modifica Modulo Commessa per periodo riferimento Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}