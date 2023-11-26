using System.IdentityModel.Tokens.Jwt;
using System.Security;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
namespace PortaleFatture.BE.Infrastructure.Gateway;

public class SelfCareHttpClient(
 IPortaleFattureOptions options,
 IHttpClientFactory httpClientFactory,
 ILogger<SelfCareHttpClient> logger) : ISelfCareHttpClient
{
    private readonly ILogger<SelfCareHttpClient> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory; 
    private readonly IPortaleFattureOptions _options = options;
    private HttpClient GetClient(string? baseAddress = null)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            var msg = "Pagopa uri certificate missing!";
            _logger.LogError(msg);
            throw new ConfigurationException(msg);
        } 

        var client = _httpClientFactory.CreateClient(nameof(SelfCareHttpClient));
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }

    public async Task<List<CertificateKey>?> GetCertificatesAsync(CancellationToken ct = default)
    {
        try
        { 
            using var client = GetClient(_options.SelfCareUri);
            var result = await client.GetAsync(_options.SelfCareCertEndpoint, ct);
            result.EnsureSuccessStatusCode();
            var jsonResponse = await result.Content.ReadAsStringAsync(ct);
            var container = jsonResponse.Deserialize<CertificateContainer>();
            return container.Keys!;
        }
        catch
        {
            var msg = "Fatal error reaching self care certificates!";
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
    }

    public async Task<CertificateKey?> GetCertificateByKidAsync(string? kid, CancellationToken ct = default)
    {
        var msg = "Fatal error reaching self care certificates!";
        if (string.IsNullOrEmpty(kid))
        {
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        var certificates = await GetCertificatesAsync(ct);
        if (certificates == null)
        {
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        var certificate = certificates.Where(x => x.Kid == kid).FirstOrDefault();
        if (certificate == null)
        {
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
        return certificate;
    }

    public JwtSecurityToken? GetSelfCareTokenAsync(string? selfcareToken, CancellationToken ct = default)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ReadToken(selfcareToken) as JwtSecurityToken; 
    }
}