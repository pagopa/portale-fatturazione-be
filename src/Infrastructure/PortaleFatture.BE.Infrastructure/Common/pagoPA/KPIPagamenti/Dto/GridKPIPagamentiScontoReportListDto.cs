using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

public sealed class GridKPIPagamentiScontoReportListDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<GridKPIPagamentiScontoReportDto>? KPIPagamentiScontoReports { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}