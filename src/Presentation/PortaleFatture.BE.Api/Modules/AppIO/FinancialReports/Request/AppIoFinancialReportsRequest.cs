namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;

/// <summary>
/// Filtri della griglia e del download "Documenti Contabili" dei documenti emessi APP IO
/// (api/appio/financialreports e api/appio/financialreports/document).
/// </summary>
public class AppIoFinancialReportsRequest
{
    /// <summary>
    /// Enti da cercare (institutionId di api/appio/contracts/name); null o vuoto = nessun filtro.
    /// </summary>
    public string[]? InstitutionIds { get; set; }

    /// <summary>
    /// Codici fiscali da cercare (valori di api/appio/financialreports/codfiscale); null o vuoto =
    /// nessun filtro.
    /// </summary>
    public string[]? CodiciFiscali { get; set; }

    /// <summary>
    /// Trimestri da cercare, nel formato 'AAAA_T'; hanno la precedenza sull'anno.
    /// </summary>
    public string[]? Quarters { get; set; }

    /// <summary>
    /// Anno ('AAAA'): senza trimestri si cercano tutti i trimestri dell'anno; senza anno né trimestri,
    /// il trimestre più recente.
    /// </summary>
    public string? Year { get; set; }
}
