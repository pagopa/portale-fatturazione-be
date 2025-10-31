using System.Text;
using System.Text.Json;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Gateway.ModuloCommessa;

public class HttpModuloCommessaEventClient : IHttpModuloCommessaEventClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public HttpModuloCommessaEventClient(string? apiKey, string? baseUrl)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));

        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey); 
    }

    public async Task<HttpResponseMessage> SendFileReadyEventAsync(string downloadUrl, string? fileVersion)
    { 
        var url = $"{_baseUrl}";

        var request = new FileReadyEventRequest
        {
            DownloadUrl = downloadUrl,
            FileVersion = fileVersion
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Errore durante la chiamata API: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
} 