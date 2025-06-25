using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public sealed class ContestazioniReportRequest
{
    public string? Anno { get; set; }
    public string? Mese { get; set; }

    private string[]? _idEnti;
    public string[]? IdEnti
    {
        get { return _idEnti; }
        set { _idEnti = value!.IsNullNotAny() ? null : value; }
    }
    private int[]? _idTipologiaReports;
    public int[]? IdTipologiaReports
    {
        get { return _idTipologiaReports; }
        set { _idTipologiaReports = value!.IsNullNotAny() ? null : value; }
    }
} 