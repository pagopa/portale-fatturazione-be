using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class ContestazioniDocumentRequest
{
    [JsonPropertyName("idReport")]
    public int? IdReport { get; set; }
    [JsonPropertyName("step")]
    public int? Step { get; set; }
} 