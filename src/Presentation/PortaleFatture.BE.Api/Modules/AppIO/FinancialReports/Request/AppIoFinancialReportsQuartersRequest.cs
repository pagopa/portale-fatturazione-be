namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Filtro dei trimestri dei documenti contabili APP IO (api/appio/financialreports/quarters).
/// </summary>
public class AppIoFinancialReportsQuartersRequest
{
    /// <summary>
    /// Anno di cui restituire i trimestri (es. "2026"); null o vuoto = tutti i trimestri.
    /// </summary>
    public string? Year { get; set; }
}
