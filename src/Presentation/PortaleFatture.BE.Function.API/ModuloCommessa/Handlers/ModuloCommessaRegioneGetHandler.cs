using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class ModuloCommessaRegioneGetHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaRegioneGetHandler", tags: ["Modulo Commessa"], Summary = "Permette di tornare le regioni da inserire nel previsionale.", Description = "Permette di tornare le regioni da inserire nel previsionale.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<ModuloCommessaRegioneDto>), Description = "Torna dto regioni da utilizzare nell'inserimento/modifica modulo commessa.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = MessageHelper.NotFoundLogging)]
    [Function("ModuloCommessaRegioneGetHandler")]
    public async Task<HttpResponseData?> ModuloCommessaRegioneGetOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/modulocommessa/regioni")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
     FunctionContext context)
    {
        var data = new ModuloCommessaRegioneInternalRequest()
        {
            Session = context.GetSession()
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaRegioneGetOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);
        var payload = new
        {
            message = "Modulo Commessa Get Regioni Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}