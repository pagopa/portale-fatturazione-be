using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture_BE_SendEmailFunction.Models.Alert;

namespace PortaleFatture_BE_SendEmailFunction.Handlers;

internal class SendAlertHandler
{

    [Function("SendAlertHandler")]
    public async Task<HttpResponseData> StartSendAlertOrchestration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {

        // deserialize req
        var requestBody = await req.ReadAsStringAsync();

        if (requestBody.IsNull())
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        // data validation
        var data = JsonConvert.DeserializeObject<AlertDataRequest>(requestBody!);

        // IdAlert cannot be negative
        if (data!.IdAlert < 0)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("SendAlertOrchestrator", data);
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