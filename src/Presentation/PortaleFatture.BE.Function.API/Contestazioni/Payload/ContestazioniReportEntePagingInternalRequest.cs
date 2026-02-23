using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ContestazioniReportEntePagingInternalRequest
{
    public string? Anno { get; set; }
    public string? Mese { get; set; }
    private int[]? _idTipologiaReports;
    public int[]? IdTipologiaReports
    {
        get { return _idTipologiaReports; }
        set { _idTipologiaReports = value?.Any() == true ? value : null; }
    }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public Session? Session { get; set; }
} 