using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Gateway;

public sealed class MicrosoftGraphGroups
{
    [JsonPropertyName("@odata.context")] 
    public string? OdataContext { get; set; }
    [JsonPropertyName("value")]
    public List<Group>? Groups { get; set; }
}

public sealed class Group
{
    [JsonPropertyName("@odata.type")]
    public string? ODataType { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; } 
}