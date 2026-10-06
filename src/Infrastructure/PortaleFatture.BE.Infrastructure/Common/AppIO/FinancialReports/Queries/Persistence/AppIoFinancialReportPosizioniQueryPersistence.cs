using System.Data;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;

/// <summary>
/// Legge le posizioni con tutte le colonne della vista, per il foglio "positions" del Financial Report.
/// </summary>
/// <param name="filtro">Il filtro della richiesta.</param>
public sealed class AppIoFinancialReportPosizioniQueryPersistence(IAppIoFinancialReportFiltro filtro) : DapperBase, IQuery<IEnumerable<AppIoFinancialReportPosizioneExcelDto>>
{
    private readonly IAppIoFinancialReportFiltro _filtro = filtro;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectPosizioni();
    private static readonly string _orderBy = AppIoFinancialReportSQLBuilder.OrderByRighe();

    /// <summary>
    /// Esegue la query con il filtro sulle posizioni.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: le viste sono nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>Le posizioni.</returns>
    public async Task<IEnumerable<AppIoFinancialReportPosizioneExcelDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var (where, parameters) = AppIoFinancialReportSQLBuilder.Where(_filtro, "p", "contract_id");

        return await ((IDatabase)this).SelectAsync<AppIoFinancialReportPosizioneExcelDto>(
           connection!,
           _sql + where + _orderBy,
           parameters,
           transaction);
    }
}
