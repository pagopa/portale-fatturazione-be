using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Gateway.ModuloCommessa;

public class FileReadyEventRequest
{
    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }
    [JsonPropertyName("fileVersion")]
    public string? FileVersion { get; set; } = "1.0.0";
}