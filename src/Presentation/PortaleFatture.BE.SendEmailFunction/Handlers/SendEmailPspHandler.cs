using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction.Handlers;

internal class SendEmailPspHandler
{

    [Function("SendEmailPspHandler")]
    public async Task<HttpResponseData> StartEmailPspOrchestration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var queryParams = req.Query;

        var anno = queryParams["trimestre"]!.Split("_")[0];
        var reinvio = queryParams["reinvio"];
        var trimestre = queryParams["trimestre"];
        var tipologia = EmailPspTipologia.Financial;
        var date = queryParams["data"];


        if (string.IsNullOrEmpty(anno) || string.IsNullOrEmpty(trimestre) || string.IsNullOrEmpty(tipologia))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var data = new EmailPspDataRequest { Anno = anno, Trimestre = trimestre, Tipologia = tipologia, Reinvio = reinvio, Date = date };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("SendEmailPspOrchestrator", data);
        var payload = new
        {
            message = "Orchestration started successfully.",
            instanceId,
            statusQueryGetUri = client.CreateHttpManagementPayload(instanceId).StatusQueryGetUri
        };

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(payload);
        return response;
    }
}