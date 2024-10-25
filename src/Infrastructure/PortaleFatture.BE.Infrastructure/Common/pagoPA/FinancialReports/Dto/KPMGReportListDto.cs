namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public sealed class KPMGReportListDto
{
    public IEnumerable<FinancialReportDto>? FinancialReports { get; set; }
    public IEnumerable<KPMGReportDto>? KPMGReports { get; set; }
}