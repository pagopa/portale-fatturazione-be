using System.Security;
using Azure.Core;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public class MicrosoftGraphHttpClient(
    IHttpClientFactory httpClientFactory,
    ILogger<MicrosoftGraphHttpClient> logger) : IMicrosoftGraphHttpClient
{
    private readonly ILogger<MicrosoftGraphHttpClient> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _endpoint = "https://graph.microsoft.com/";

    private HttpClient GetClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(MicrosoftGraphHttpClient));
        client.BaseAddress = new Uri(_endpoint!);
        return client;
    }
    public async Task<Dictionary<string, string?>> GetGroupsAsync(string azureADAccessToken, CancellationToken ct = default)
    {
        try
        {
            using var client = GetClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + azureADAccessToken);
            var result = await client.GetAsync($"v1.0/me/memberOf", ct); 
            result.EnsureSuccessStatusCode();
            var jsonResponse = await result.Content.ReadAsStringAsync(ct);
            var graph = jsonResponse.Deserialize<MicrosoftGraphGroups>();
            return graph.Groups!.Select(t => new { t.Id, t.DisplayName })
                   .ToDictionary(t => t.Id, t => t.DisplayName);
        }
        catch
        {
            var msg = "Fatal error reaching pagoPA certificates!";
            _logger.LogError(msg);
            throw new SecurityException(msg);
        }
    }
}