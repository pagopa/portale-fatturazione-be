using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;

public sealed class OrchestratoreDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<OrchestratoreItem>? Items { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
} 