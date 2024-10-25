using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
public class PipelineParameters()
{
    [JsonPropertyName("AnnoRiferimento")]
    public int AnnoRiferimento { get; set; }

    [JsonPropertyName("MeseRiferimento")]
    public int MeseRiferimento { get; set; }

    [JsonPropertyName("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }
}