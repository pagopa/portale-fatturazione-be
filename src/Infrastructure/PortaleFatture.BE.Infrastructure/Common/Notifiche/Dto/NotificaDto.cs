using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;

public sealed class NotificaDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<SimpleNotificaDto>? Notifiche { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}