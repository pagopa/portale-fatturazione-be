using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction.Handlers; 
internal class RichiestaNotificheHandler
{

    [Function("RichiestaNotificheHandler")]
    public async Task<HttpResponseData> StartRichiestaNotificheOrchestration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("RichiestaNotificheHandler");
        var requestBody = await req.ReadAsStringAsync();
 
        var data = JsonConvert.DeserializeObject<NotificheRicercaRequest>(requestBody!);  
        logger.LogError("Starting orchestration with data: {Data}", data.Serialize());

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("RichiestaNotificheOrchestrator", data);
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