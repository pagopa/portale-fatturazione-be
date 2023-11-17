using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Core.Auth.SelfCare;

public class SelfCareRole
{
    [JsonPropertyName("partyRole")]
    public string? PartyRole { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("product")]
    public string? Product { get; set; }
} 
