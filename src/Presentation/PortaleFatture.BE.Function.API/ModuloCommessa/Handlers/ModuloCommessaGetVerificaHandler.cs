using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;
 
public class ModuloCommessaGetVerificaHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaGetVerificaHandler", tags: ["Modulo Commessa"], Summary = "Permette di tornare verifica se ci sono moduli obbligatori da inserire.", Description = "Permette di tornare verifica se ci sono moduli obbligatori da inserire.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(bool), Description = "Torna true se esistono obbligatori altrimenti false.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("ModuloCommessaGetVerificaHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetByAnnoMeseDettaglioOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/modulocommessa/obbligatori/verifica")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    { 
        var data = new ModuloCommessaGetVerificaInternalRequest()
        { 
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaGetVerificaOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Modulo Commessa Get Verifica Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}