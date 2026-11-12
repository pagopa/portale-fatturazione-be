namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Trimestre di cui scaricare il file del financial report (api/appio/financialreports/blob/download).
/// </summary>
public class AppIoFinancialReportsFileRequest
{
    /// <summary>
    /// Anno di riferimento. Obbligatorio.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Trimestre di riferimento, da 1 a 4. Obbligatorio.
    /// </summary>
    public int? Quarter { get; set; }
}
