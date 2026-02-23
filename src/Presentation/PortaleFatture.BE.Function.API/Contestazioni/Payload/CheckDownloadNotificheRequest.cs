using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class CheckDownloadNotificheRequest
{
    [JsonPropertyName("dataVerifica")]
    public DateTime? DataVerifica { get; set; }

    [JsonPropertyName("anno")]
    public int Anno { get; set; }

    [JsonPropertyName("mese")]
    public int Mese { get; set; }
} 