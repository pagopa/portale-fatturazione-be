using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

 
public sealed class ReportNotificheListCountDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<ReportNotificheListDto>? Items { get; set; }
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}