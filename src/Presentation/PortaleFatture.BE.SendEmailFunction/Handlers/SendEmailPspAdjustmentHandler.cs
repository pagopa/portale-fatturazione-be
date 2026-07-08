using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;
using PortaleFatture_BE_SendEmailFunction.Orchestrators;

namespace PortaleFatture_BE_SendEmailFunction.Handlers;

internal class SendEmailPspAdjustmentHandler
{

    [Function(nameof(SendEmailPspAdjustmentHandler))]
    public async Task<HttpResponseData> StartEmailPspAdjustmentOrchestration(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var queryParams = req.Query;

        bool? preview = bool.TryParse(queryParams["preview"], out var previewValue) ? previewValue : null;

        var data = new EmailPspAdjustmentDataRequest { Preview = preview };

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(SendEmailPspAdjustmentOrchestrator), data);
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