using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ReportContestazioneStepsRequest
{
    [JsonPropertyName("idReport")]
    public int IdReport { get; set; }
} 