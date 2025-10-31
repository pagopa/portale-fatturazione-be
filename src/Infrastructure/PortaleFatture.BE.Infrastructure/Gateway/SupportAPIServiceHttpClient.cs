using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Exceptions;
namespace PortaleFatture.BE.Infrastructure.Gateway;

public class SupportAPIServiceHttpClient(
 IPortaleFattureOptions options,
 IHttpClientFactory httpClientFactory,
 ILogger<SupportAPIServiceHttpClient> logger) : ISupportAPIServiceHttpClient
{
    private readonly ILogger<SupportAPIServiceHttpClient> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IPortaleFattureOptions _options = options;
    private enum Status
    {
        REQUEST,
        TOBEVALIDATED,
        PENDING,
        COMPLETED,
        FAILED,
        REJECTED,
        DELETED
    } 
    private HttpClient GetClient(string? baseAddress = null)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            var msg = "Support API Service uri missing!";
            _logger.LogError(msg);
            throw new ConfigurationException(msg);
        }

        var client = _httpClientFactory.CreateClient(nameof(SupportAPIServiceHttpClient));
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    public async Task<(bool Success, string Message)> UpdateRecipientCode(EnteContrattoDto ente, string? codiceSDI, CancellationToken ct = default)
    {
        string? msg;
        try
        {
            using var client = GetClient(_options.SupportAPIService!.Endpoint);
            var queryParams = new Dictionary<string, string>()
            {
                { "recipientCode", codiceSDI! }
            };

            var data = new object();
 
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.SupportAPIService.AuthToken);

            var queryString = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync(ct);
            var apiUrl = $"{_options.SupportAPIService.RecipientCodeUri!.Replace("{onboardingId}", ente.IdContratto)}?{queryString}";

            var response = await client.PutAsJsonAsync(apiUrl, data, ct);
            var status = response.StatusCode;

            switch (status)
            {
                case HttpStatusCode.OK: // 200
                case HttpStatusCode.NoContent: // 204
                    return (true, string.Empty);
                case HttpStatusCode.BadRequest:
                      msg = $"Modifica Codice SDI per l'ente {ente.RagioneSociale} con id:{ente.IdEnte} e onboardingId:{ente.IdContratto} fallita: ente non presente";
                    _logger.LogError($"{msg}");
                    break;
                default:
                    msg = $"Modifica Codice SDI per l'ente {ente.RagioneSociale} con id:{ente.IdEnte} e onboardingId:{ente.IdContratto} fallita: servizio risponde con {status}";
                    _logger.LogError($"{msg}");
                    break;
            }
        }
        catch
        {
            msg = $"Modifica Codice SDI per l'ente {ente.RagioneSociale} con id:{ente.IdEnte} e onboardingId:{ente.IdContratto} fallita: servizio non risponde";
            _logger.LogError($"{msg}"); 
        }
        msg = "La modifica non è andata a buon fine, ritentare l'inserimento oppure contattare l'assistenza.";
        return (false, msg);
    }
}