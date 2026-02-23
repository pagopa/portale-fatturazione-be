using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;

public static class DurableTaskStatus
{
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Terminated = "Terminated";
    public const string Pending = "Pending";
    public const string WaitingForExternalEvent = "WaitingForExternalEvent";
    public const string ContinuedAsNew = "ContinuedAsNew";
    public const string Canceled = "Canceled";
    public const string NotFound = "NotFound"; 

  public static int GetHttpStatusCode(string status)
    {
        return status switch
        {
            Running or Pending or WaitingForExternalEvent => 202,// Accepted (The task is in progress)
            Completed => 200,// OK (The task has completed successfully)
            Failed => 500,// Internal Server Error (The task has failed)
            Terminated => 400,// Bad Request (The task was explicitly terminated)
            ContinuedAsNew => 202,// Accepted (The task is continuing as new)
            Canceled => 408,// Request Timeout (The task was canceled)
            NotFound => 404,// Not Found (The instance was not found)
            _ => 500,// Internal Server Error (Unknown or unhandled status)
        };
    }
}

public class FunctionResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }
    [JsonPropertyName("statusQueryGetUri")]
    public string? StatusQueryGetUri { get; set; }
}

public class DurableFunctionResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("runtimeStatus")]
    public string? RuntimeStatus { get; set; }

    [JsonPropertyName("input")]
    public Input? Input { get; set; }

    [JsonPropertyName("customStatus")]
    public string? CustomStatus { get; set; }

    [JsonPropertyName("output")]
    public string? Output { get; set; }

    [JsonPropertyName("createdTime")]
    public DateTime? CreatedTime { get; set; }

    [JsonPropertyName("lastUpdatedTime")]
    public DateTime? LastUpdatedTime { get; set; }
}

public class Input
{
    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }

    [JsonPropertyName("prodotto")]
    public string? Prodotto { get; set; }

    [JsonPropertyName("cap")]
    public string? Cap { get; set; }

    [JsonPropertyName("profilo")]
    public string? Profilo { get; set; }

    [JsonPropertyName("tipoNotifica")]
    public int? TipoNotifica { get; set; }

    [JsonPropertyName("statoContestazione")]
    public int[]? StatoContestazione { get; set; }

    [JsonPropertyName("iun")]
    public string? Iun { get; set; }

    [JsonPropertyName("recipientId")]
    public string? RecipientId { get; set; }

    [JsonPropertyName("idEnte")]
    public string? IdEnte { get; set; }

    [JsonPropertyName("ragioneSociale")]
    public string? RagioneSociale { get; set; }

    [JsonPropertyName("idContratto")]
    public string? IdContratto { get; set; }

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("idReport")]
    public int IdReport { get; set; }
}

public class FunctionNotificheCaller(IPortaleFattureOptions options, ILogger<FunctionNotificheCaller> logger, IHttpClientFactory httpClientFactory) : IFunctionNotificheCaller
{

    private readonly ILogger<FunctionNotificheCaller> _logger = logger;
    private readonly IPortaleFattureOptions _options = options;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    public async Task<FunctionResponse?> CallAzureFunction(dynamic request)
    {
        var functionUrl = _options.AzureFunction!.NotificheUri!;
        var functionKey = _options.AzureFunction!.AppKey!;

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(FunctionNotificheCaller));

            client.DefaultRequestHeaders.Add("x-functions-key", functionKey);
            string jsonData = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(functionUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var functionResponse = result.Deserialize<FunctionResponse>();

                _logger.LogInformation($"Message: {functionResponse.Message}");
                _logger.LogInformation($"InstanceId: {functionResponse.InstanceId}");
                _logger.LogInformation($"StatusQueryGetUri: {functionResponse.StatusQueryGetUri}");

                return functionResponse;
            }
            else
            {
                _logger.LogError($"Error calling function. Status code: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            return null;
        }
    }

    public async Task<(DurableFunctionResponse?, string?)> CallDurableFunctionWebhook(string functionUrl)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(FunctionNotificheCaller));

            var functionKey = _options.AzureFunction?.AppKey;
            client.DefaultRequestHeaders.Add("x-functions-key", functionKey);

            var response = await client.GetAsync(functionUrl);
            var result = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return (result.Deserialize<DurableFunctionResponse>(), "ok");
            }
            else
            {
                _logger.LogWarning("Empty response body received from Durable Task.");
                return (null, result + "| Empty response body received from Durable Task. |" + response.StatusCode.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred: {ex.Message}, StackTrace: {ex.StackTrace}");
            return (null, ex.Message);
        }
    }
}