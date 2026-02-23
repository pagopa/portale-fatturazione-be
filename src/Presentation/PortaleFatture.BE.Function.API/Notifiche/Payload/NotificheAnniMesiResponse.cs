using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Function.API.Notifiche.Payload;

public sealed class NotificheAnniMesiResponse
{
    [JsonPropertyOrder(1)]
    public int? Anno { get; set; }

    [JsonPropertyOrder(-1)]
    public int? Mese { get; set; }

    [JsonPropertyOrder(-2)]
    public string? Descrizione { get; set; }
} 