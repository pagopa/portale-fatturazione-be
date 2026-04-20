using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using MimeKit.Cryptography;
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
        var tipoComunicazione = queryParams["tipoComunicazione"];
        var fase = queryParams["fase"];


        if (string.IsNullOrEmpty(anno) || string.IsNullOrEmpty(mese) || string.IsNullOrEmpty(tipologiafattura) || string.IsNullOrEmpty(tipoComunicazione))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        int? faseValue = null;
        if (string.Equals(tipoComunicazione, "fattura", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(fase) || !int.TryParse(fase, out var parsedFase))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            faseValue = parsedFase;
        }
        else if (!string.IsNullOrEmpty(fase) && int.TryParse(fase, out var parsedFase))
        {
            faseValue = parsedFase;
        }

        if (faseValue is not null && faseValue != 1 && faseValue != 2)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var data = new EmailRelDataRequest { Anno = anno, Mese = mese, TipologiaFattura = tipologiafattura, TipoComunicazione = tipoComunicazione, Fase = faseValue };

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