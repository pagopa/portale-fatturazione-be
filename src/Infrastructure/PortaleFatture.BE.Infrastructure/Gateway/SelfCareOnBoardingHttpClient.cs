using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
namespace PortaleFatture.BE.Infrastructure.Gateway;

public class SelfCareOnBoardingHttpClient(
 IPortaleFattureOptions options,
 IHttpClientFactory httpClientFactory,
 ILogger<SelfCareOnBoardingHttpClient> logger) : ISelfCareOnBoardingHttpClient
{
    private readonly ILogger<SelfCareOnBoardingHttpClient> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IPortaleFattureOptions _options = options;
    private HttpClient GetClient(string? baseAddress = null)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            var msg = "SelfCare OnBoarding uri missing!";
            _logger.LogError(msg);
            throw new ConfigurationException(msg);
        }

        var client = _httpClientFactory.CreateClient(nameof(SelfCareOnBoardingHttpClient));
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    public async Task<(bool Success, string Message)> RecipientCodeVerification(EnteContrattoDto ente, string? codiceSDI, bool skipVerifica, CancellationToken ct = default)
    {
        // validare solo se InstitutionType == PA
        if (ente.InstitutionType!.ToLower() != Profilo.PubblicaAmministrazione.ToLower() || skipVerifica)
            return (true, string.Empty);

        try
        {
            using var client = GetClient(_options.SelfCareOnBoarding!.Endpoint);
            var queryParams = new Dictionary<string, string>()
            {
                { "originId", ente.CodiceIPA! },
                { "recipientCode", codiceSDI! }
            };

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.SelfCareOnBoarding.AuthToken);


            var queryString = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync(ct);
            var apiUrl = $"{_options.SelfCareOnBoarding.RecipientCodeUri}?{queryString}";
            var result = await client.GetAsync(apiUrl, ct);
            var status = result.StatusCode;
            switch (status)
            {
                case System.Net.HttpStatusCode.OK:
                    var jsonResponse = await result.Content.ReadAsStringAsync(ct);
                    var value = jsonResponse.Deserialize<string>();
                    switch (value)
                    {
                        case "ACCEPTED":
                            return (true, string.Empty);
                        case "DENIED_NO_BILLING":
                            {
                                var msg = "Il codice SDI presente è associato al codice fiscale di un ente che non ha il servizio di fatturazione attivo.";
                                _logger.LogError(msg + $" {ente.RagioneSociale} IdEnte: {ente.IdEnte}  codice verificato:{codiceSDI}");
                                return (false, msg);
                            }

                        default: //DENIED_NO_ASSOCIATION
                            {
                                var msg = "Il codice SDI presente non è associato al tuo ente.";
                                _logger.LogError(msg + $" {ente.RagioneSociale} IdEnte: {ente.IdEnte}  codice verificato:{codiceSDI}");
                                return (false, msg);
                            }
                    }
                case System.Net.HttpStatusCode.NotFound:
                    {
                        var msg = "Il codice SDI presente non è valido o non esiste.";
                        _logger.LogError(msg + $" {ente.RagioneSociale} IdEnte: {ente.IdEnte}  codice verificato:{codiceSDI}");
                        return (false, msg);
                    }
                default:
                    {
                        var msg = $"Codice risposta dal server non mappato: {status}.";
                        _logger.LogError(msg + $" {ente.RagioneSociale} IdEnte: {ente.IdEnte}  codice verificato:{codiceSDI}");
                        break;
                    }
            }
        }
        catch
        {
            var msg = "Fatal error reaching SelfCare OnBoarding!";
            _logger.LogError(msg + $" {ente.RagioneSociale} IdEnte: {ente.IdEnte}  codice verificato:{codiceSDI}");
            throw new ValidationException(msg);
        }
        return (false, "Si è verificato un errore nella verifica del codice SDI.");
    }
}