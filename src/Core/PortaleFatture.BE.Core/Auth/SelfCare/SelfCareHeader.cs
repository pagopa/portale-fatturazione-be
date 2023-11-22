using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Auth.SelfCare;

public class SelfCareHeader
{
    [JsonPropertyName("kid")]
    public string? Kid { get; set; }

    [JsonPropertyName("typ")]
    public string? Typ { get; set; }

    [JsonPropertyName("alg")]
    public string? Alg { get; set; }
} 