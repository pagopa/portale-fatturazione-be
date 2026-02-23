using System.Text.Json.Serialization;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Function.API.Contestazioni.Payload;

public sealed class ContestazioniReportEntePagingRequest
{
    [JsonPropertyName("anno")]
    public string? Anno { get; set; }

    [JsonPropertyName("mese")]
    public string? Mese { get; set; }

    private int[]? _idTipologiaReports;

    [JsonPropertyName("idTipologiaReports")]
    public int[]? IdTipologiaReports
    {
        get { return _idTipologiaReports; }
        set { _idTipologiaReports = value!.IsNullNotAny() ? null : value; }
    }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 10;
} 