using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Function.API.Contestazioni.Handlers; 

internal class CheckDownloadNotificheHandler
{
    [OpenApiOperation(operationId: "CheckDownloadNotificheHandler", tags: ["Contestazioni"], Summary = "Ritorna OK/KO per download notifiche per il periodo di riferimento. Se true bisogna aggiornare il file notifiche facendo il download sull'endpoint notifiche, altrimenti no.", Description = "Ritorna OK/KO per download notifiche per il periodo di riferimento. Se true bisogna aggiornare il file notifiche facendo il download sull'endpoint notifiche, altrimenti no.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CheckDownloadNotificheRequest), Description = "Request con data ultimo download notifiche o caricamente contestazioni")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(CheckDownloadNotificheDto), Description = "Ritorna DataMassimaContestazione, NotificheDaScaricare (OK/KO), CiSonoContestazioni(SI/NO)")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]
    [Function("CheckDownloadNotificheHandler")]
    public async Task<HttpResponseData> CheckDownloadNotificheOrchestration(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/contestazioni/notifiche/check/download")] HttpRequestData req,
    [DurableClient] DurableTaskClient client,
    FunctionContext context)
    {
        var requestBody = context.GetItem<string>("requestBody");
        var request = requestBody!.Deserialize<CheckDownloadNotificheRequest>();
        var data = new CheckDownloadNotificheInternalRequest()
        {
            Session = context.GetSession(),
            DataVerifica = request.DataVerifica,
            Anno  = request.Anno,
            Mese = request.Mese
        };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("CheckDownloadNotificheOrchestrator", data);
        var originalPayload = client.CreateHttpManagementPayload(instanceId);
        var customStatusUri = context.GetUri(originalPayload.StatusQueryGetUri!);

        var payload = new
        {
            message = "Check Download Notifiche Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = customStatusUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}