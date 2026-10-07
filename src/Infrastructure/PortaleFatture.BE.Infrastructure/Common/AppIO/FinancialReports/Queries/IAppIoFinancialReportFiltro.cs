namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;

/// <summary>
/// Filtro comune alla griglia e ai due download dei documenti contabili APP IO, così che le tre
/// rotte selezionino sempre le stesse righe.
/// </summary>
public interface IAppIoFinancialReportFiltro
{
    /// <summary>
    /// Contratti da cercare; null o vuoto = nessun filtro.
    /// </summary>
    string[]? ContractIds { get; }

    /// <summary>
    /// Trimestri da cercare, nel formato 'AAAA_T'. Hanno la precedenza su <see cref="Year"/>.
    /// </summary>
    string[]? YearQuarter { get; }

    /// <summary>
    /// Anno: senza trimestri si cercano tutti i trimestri dell'anno. Se mancano anche i trimestri,
    /// si cerca il trimestre più recente.
    /// </summary>
    string? Year { get; }
}
