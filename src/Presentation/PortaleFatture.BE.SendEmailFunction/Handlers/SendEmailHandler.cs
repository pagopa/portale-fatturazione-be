using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Handlers;

internal class SendEmailHandler
{

    [Function("SendEmailHandler")]
    public async Task<HttpResponseData> StartEmailOrchestration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var queryParams = req.Query;

        var anno = queryParams["anno"];
        var mese = queryParams["mese"];
        var tipologiafattura = queryParams["tipologiafattura"];


        if (string.IsNullOrEmpty(anno) || string.IsNullOrEmpty(mese) || string.IsNullOrEmpty(tipologiafattura))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var data = new EmailRelDataRequest { Anno = anno, Mese = mese, TipologiaFattura = tipologiafattura };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("SendEmailOrchestrator", data);
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