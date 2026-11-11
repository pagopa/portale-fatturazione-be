namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Documento di cui leggere o scaricare il dettaglio (api/appio/financialreports/detail e
/// api/appio/financialreports/detail/download): una riga padre della griglia.
/// </summary>
public class AppIoFinancialReportsDettaglioRequest
{
    /// <summary>
    /// Contratto (contractId della riga della griglia). Obbligatorio.
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Trimestre nel formato 'AAAA_T' (yearQuarter della riga della griglia). Obbligatorio.
    /// </summary>
    public string? YearQuarter { get; set; }
}
