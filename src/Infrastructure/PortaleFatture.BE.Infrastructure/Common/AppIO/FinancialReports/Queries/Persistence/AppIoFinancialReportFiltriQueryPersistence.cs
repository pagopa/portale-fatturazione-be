using System.Data;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;

/// <summary>
/// Legge gli anni distinti da [be].[vwAppioFinalReportFiltroAnno].
/// </summary>
/// <param name="command">La query (senza filtri).</param>
public sealed class AppIoFinancialReportYearsQueryPersistence(AppIoFinancialReportYearsQuery command) : DapperBase, IQuery<IEnumerable<int>>
{
    private readonly AppIoFinancialReportYearsQuery _command = command;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectYears();

    /// <summary>
    /// Restituisce gli anni, dal più recente.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>Gli anni.</returns>
    public async Task<IEnumerable<int>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        => await ((IDatabase)this).SelectAsync<int>(connection!, _sql, new { }, transaction);
}

/// <summary>
/// Legge i codici fiscali distinti da [be].[vwAppioFinalReportFiltroCodFiscaleEnte].
/// </summary>
/// <param name="command">La query (senza filtri).</param>
public sealed class AppIoFinancialReportCodiciFiscaliQueryPersistence(AppIoFinancialReportCodiciFiscaliQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly AppIoFinancialReportCodiciFiscaliQuery _command = command;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectCodiciFiscali();

    /// <summary>
    /// Restituisce i codici fiscali, in ordine crescente.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>I codici fiscali.</returns>
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        => await ((IDatabase)this).SelectAsync<string>(connection!, _sql, new { }, transaction);
}

/// <summary>
/// Legge le righe del dettaglio di un contratto in un trimestre da [be].[vwAppioFinalReportDetails].
/// </summary>
/// <param name="command">La query con contratto e trimestre.</param>
public sealed class AppIoFinancialReportDettaglioQueryPersistence(AppIoFinancialReportQueryGetDettaglio command) : DapperBase, IQuery<IEnumerable<Dto.AppIoFinancialReportDettaglioRigaDto>>
{
    private readonly AppIoFinancialReportQueryGetDettaglio _command = command;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectDettaglio();

    /// <summary>
    /// Restituisce le righe, ordinate per documento e articolo.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>Le righe del dettaglio.</returns>
    public async Task<IEnumerable<Dto.AppIoFinancialReportDettaglioRigaDto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
        => await ((IDatabase)this).SelectAsync<Dto.AppIoFinancialReportDettaglioRigaDto>(
            connection!,
            _sql,
            new { _command.ContractId, _command.YearQuarter },
            transaction);
}
