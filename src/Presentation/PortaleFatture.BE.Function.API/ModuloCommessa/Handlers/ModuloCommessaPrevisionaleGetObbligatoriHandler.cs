using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class ModuloCommessaPrevisionaleGetObbligatoriHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaPrevisionaleGetObbligatoriHandler", tags: ["Modulo Commessa"], Summary = "Permette di tornare i dati obbligatori da inserire per il modulo commessa.", Description = "Permette di tornare i dati obbligatori da inserire per il modulo commessa.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ModuloCommessaPrevisionaleObbligatoriResponse), Description = "Torna obbligatori da utilizzare nell'inserimento/modifica modulo commessa.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("ModuloCommessaPrevisionaleGetObbligatoriHandler")]
    public async Task<HttpResponseData?> ModuloCommessaPrevisionaleGetObbligatoriOrchestrator(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/modulocommessa/obbligatori/lista")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    {
        var data = new ModuloCommessaPrevisionaleObbligatoriInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaPrevisionaleGetObbligatoriOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Modulo Commessa Get PRevisionale Obbligatori Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}