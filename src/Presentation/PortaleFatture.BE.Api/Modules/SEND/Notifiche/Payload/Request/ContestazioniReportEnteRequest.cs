using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public sealed class ContestazioniReportEnteRequest
{
    public string? Anno { get; set; }
    public string? Mese { get; set; } 

    private int[]? _idTipologiaReports;
    public int[]? IdTipologiaReports
    {
        get { return _idTipologiaReports; }
        set { _idTipologiaReports = value!.IsNullNotAny() ? null : value; }
    }
} 