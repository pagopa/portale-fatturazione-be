namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;

/// <summary>
/// Voce del filtro "Trimestre" dei documenti contabili APP IO.
/// </summary>
public class AppIoFinancialReportsQuartersResponse
{
    /// <summary>
    /// Valore del trimestre da rimandare nei filtri, nel formato 'AAAA_T' (es. "2026_1").
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Etichetta da mostrare (es. "Q1").
    /// </summary>
    public string? Quarter { get; set; }
}
