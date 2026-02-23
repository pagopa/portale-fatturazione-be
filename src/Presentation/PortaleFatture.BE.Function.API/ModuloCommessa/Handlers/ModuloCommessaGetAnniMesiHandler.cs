using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Function.API.ModuloCommessa.Handlers;


public class ModuloCommessaGetAnniMesiHandler
{
    [OpenApiOperation(operationId: "ModuloCommessaGetAnniMesi", tags: ["Modulo Commessa"], Summary = "Permette di tornare i vari periodi storici in cui è stata inserito un modulo commessa e quelli previsionali.", Description = "Permette di tornare i vari periodi storici in cui è stata inserito un modulo commessa, in particolare anno e mese.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ModuloCommessaGetAnniMesiRequest), Description = "Modulo Commessa request anno")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<string>), Description = "Torna gli anni per i moduli commessa previsionali.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("ModuloCommessaGetAnniMesiHandler")]
    public async Task<HttpResponseData?> ModuloCommessaGetAnniMesiOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get",  Route = "v1/modulocommessa/periodo")] HttpRequestData req,
    [DurableClient] DurableTaskClient client, 
     FunctionContext context)
    { 
        var data = new ModuloCommessaGetAnniMesiRequest()
        {
            Session = context.GetSession(),
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("ModuloCommessaGetAnniMesiOrchestrator", data); 
        var originalPayload = client.CreateHttpManagementPayload(instanceId); 
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!); 
        var payload = new
        {
            message = "Modulo Commessa Anni Mesi Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}