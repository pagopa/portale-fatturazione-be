using System.Data;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;

/// <summary>
/// Legge le righe di [be].[vwAppioFinancialReport], per il foglio "financial-report" del Financial Report.
/// </summary>
/// <param name="filtro">Il filtro della richiesta.</param>
public sealed class AppIoFinancialReportValoriQueryPersistence(IAppIoFinancialReportFiltro filtro) : DapperBase, IQuery<IEnumerable<AppIoFinancialReportValoreDto>>
{
    private readonly IAppIoFinancialReportFiltro _filtro = filtro;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectValori();
    private static readonly string _orderBy = AppIoFinancialReportSQLBuilder.OrderByValori();

    /// <summary>
    /// Esegue la query con il filtro sul financial report (il contratto qui è recipient_id).
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: le viste sono nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>Le righe del financial report.</returns>
    public async Task<IEnumerable<AppIoFinancialReportValoreDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var (where, parameters) = AppIoFinancialReportSQLBuilder.Where(_filtro, "r", "recipient_id");

        return await ((IDatabase)this).SelectAsync<AppIoFinancialReportValoreDto>(
           connection!,
           _sql + where + _orderBy,
           parameters,
           transaction);
    }
}
