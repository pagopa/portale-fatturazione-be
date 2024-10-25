using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class GridFinancialReportListDto
{
    [JsonPropertyOrder(-1)]
    public IEnumerable<GridFinancialReportDto>? FinancialReports { get; set; }

    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}