namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Filtri della griglia e dei due download dei documenti contabili APP IO
/// (api/appio/financialreports, .../document, .../detail/download).
/// </summary>
public class AppIoFinancialReportsRequest
{
    /// <summary>
    /// Contratti da cercare; null o vuoto = nessun filtro.
    /// </summary>
    public string[]? ContractIds { get; set; }

    /// <summary>
    /// Trimestri da cercare, nel formato 'AAAA_T'; hanno la precedenza sull'anno.
    /// </summary>
    public string[]? Quarters { get; set; }

    /// <summary>
    /// Anno: senza trimestri si cercano tutti i trimestri dell'anno; senza anno né trimestri, il
    /// trimestre più recente.
    /// </summary>
    public string? Year { get; set; }
}
