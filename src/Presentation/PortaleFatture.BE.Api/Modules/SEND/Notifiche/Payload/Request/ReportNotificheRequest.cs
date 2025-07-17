namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public sealed class ReportNotificheRequest
{
    public DateTime? Init { get; set; }
    public DateTime? End { get; set; }
    public int? Ordinamento { get; set; } = 1;
} 