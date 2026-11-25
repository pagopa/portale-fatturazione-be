namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Trimestri di cui scaricare il file del financial report (api/appio/financialreports/blob/download).
/// </summary>
public class AppIoFinancialReportsFileRequest
{
    /// <summary>
    /// Anno di riferimento ('AAAA'). Obbligatorio.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Trimestri nel formato 'AAAA_T' (es. ["2026_1","2026_2"]), gli stessi valori del filtro
    /// "Trimestre"; devono essere tutti dell'anno indicato. Assenti o vuoti: tutti i trimestri
    /// dell'anno. Con un trimestre si riceve il file; altrimenti uno ZIP con un file per trimestre.
    /// </summary>
    public string[]? Quarters { get; set; }
}
