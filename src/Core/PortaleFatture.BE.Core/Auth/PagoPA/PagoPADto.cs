using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Auth.PagoPA;

public class PagoPADto
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; } 

    [JsonPropertyName("roles")]
    public IEnumerable<string>? Roles { get; set; }

    [JsonPropertyName("groups")]
    public IEnumerable<string>? Groups { get; set; }
} 