using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class NotificheRiceraVerificaRequest
{
    [JsonPropertyName("statusQueryGetUri")]
    public string? StatusQueryGetUri { get; set; }
    [JsonPropertyName("idEnte")]
    public string? IdEnte { get; set; }
} 