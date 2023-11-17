using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Auth.SelfCare; 
public class SelfCareOrganization
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("fiscal_code")]
    public string? FiscalCode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("roles")]
    public List<SelfCareRole>? Roles { get; set; }

    [JsonPropertyName("groups")]
    public List<string>? Groups { get; set; }
} 