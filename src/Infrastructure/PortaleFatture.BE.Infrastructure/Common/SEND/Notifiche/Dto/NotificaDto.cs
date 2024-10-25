using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

public sealed class NotificaDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleNotificaDto>? Notifiche { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}

public sealed class NotificaRECCONDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<RECCONNotificaDto>? Notifiche { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}