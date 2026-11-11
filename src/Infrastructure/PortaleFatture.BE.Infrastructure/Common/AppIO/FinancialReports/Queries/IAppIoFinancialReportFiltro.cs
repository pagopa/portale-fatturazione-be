namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Filtro comune alla griglia e al download dei documenti emessi APP IO, così che le due rotte
/// selezionino sempre le stesse righe di [be].[vwAppioFinalReport].
/// </summary>
public interface IAppIoFinancialReportFiltro
{
    /// <summary>
    /// Enti da cercare (internalistitutionid, valori del filtro "Nome Ente"); null o vuoto = nessun filtro.
    /// </summary>
    string[]? InstitutionIds { get; }

    /// <summary>
    /// Codici fiscali degli enti da cercare; null o vuoto = nessun filtro.
    /// </summary>
    string[]? CodiciFiscali { get; }

    /// <summary>
    /// Trimestri da cercare, nel formato 'AAAA_T'. Hanno la precedenza su <see cref="Year"/>.
    /// </summary>
    string[]? YearQuarter { get; }

    /// <summary>
    /// Anno ('AAAA'): senza trimestri si cercano tutti i trimestri dell'anno. Se mancano anche i
    /// trimestri, si cerca il trimestre più recente.
    /// </summary>
    string? Year { get; }
}
