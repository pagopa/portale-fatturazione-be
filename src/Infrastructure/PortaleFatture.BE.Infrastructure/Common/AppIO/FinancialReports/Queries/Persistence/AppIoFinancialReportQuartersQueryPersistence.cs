using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;

/// <summary>
/// Legge i trimestri distinti dei documenti emessi APP IO da [be].[vwAppioFinalReport],
/// eventualmente limitati a un anno.
/// </summary>
/// <remarks>
/// Non usa [be].[vwAppioFinalReportFiltroTrimestre]: quella vista restituisce i numeri di trimestre
/// (1-4) senza anno, mentre il filtro deve proporre solo i trimestri presenti nell'anno scelto, nel
/// formato 'AAAA_T' accettato dalla griglia.
/// </remarks>
/// <param name="command">La query con l'anno facoltativo.</param>
public sealed class AppIoFinancialReportQuartersQueryPersistence(AppIoFinancialReportQuartersQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly AppIoFinancialReportQuartersQuery _command = command;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectQuarters();
    private static readonly string _groupBy = AppIoFinancialReportSQLBuilder.GroupByQuarters();

    /// <summary>
    /// Restituisce i trimestri distinti, dal più recente; con l'anno valorizzato solo quelli che
    /// iniziano con 'AAAA_'.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: le viste sono nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>I trimestri nel formato 'AAAA_T', in ordine decrescente.</returns>
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where = string.Empty;
        dynamic parameters = new ExpandoObject();

        if (!string.IsNullOrEmpty(_command.Year))
        {
            // '[_]' perché in LIKE il trattino basso è un jolly
            where = " WHERE [trimestre] LIKE @Year + '[_]%'";
            parameters.Year = _command.Year;
        }

        return await ((IDatabase)this).SelectAsync<string>(
           connection!,
           _sql + where + _groupBy,
           parameters,
           transaction);
    }
}
