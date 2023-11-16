using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public class PagoPaHttpClient : IPagoPaHttpClient
{
    private readonly ILogger<PagoPaHttpClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<PortaleFattureOptions> _optionsMonitor;
    private HttpClient GetClient(string? baseAddress = null)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            var msg = "Pagopa uri certificate missing!";
            _logger.LogError(msg);
            throw new ConfigurationException(msg);
        }


        var client = _httpClientFactory.CreateClient(nameof(PagoPaHttpClient));
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    public PagoPaHttpClient(
     IOptionsMonitor<PortaleFattureOptions> optionsMonitor,
     IHttpClientFactory httpClientFactory,
     ILogger<PagoPaHttpClient> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _optionsMonitor = optionsMonitor;
    }
    public async Task<List<CertificateKey>> GetCertificatesAsync(CancellationToken ct = default)
    {
        try
        {
            var options = _optionsMonitor.CurrentValue;
            using var client = GetClient(options.SelfCareUri);
            var result = await client.GetAsync(options.SelfCareCertEndpoint, ct);
            result.EnsureSuccessStatusCode();
            var jsonResponse = await result.Content.ReadAsStringAsync(ct);
            var container = jsonResponse.Deserialize<CertificateContainer>();
            return container.Keys!;
        }
        catch
        {
            var msg = "Fatal error reaching self care certificates!";
            _logger.LogError(msg);
            throw new ConfigurationException(msg);
        }
    }
}