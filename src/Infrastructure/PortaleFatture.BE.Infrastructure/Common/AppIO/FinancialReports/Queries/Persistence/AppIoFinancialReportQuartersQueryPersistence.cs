using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;

/// <summary>
/// Legge i trimestri distinti dei documenti contabili APP IO, eventualmente limitati a un anno.
/// </summary>
/// <param name="command">La query con l'anno facoltativo.</param>
public sealed class AppIoFinancialReportQuartersQueryPersistence(AppIoFinancialReportQuartersQuery command) : DapperBase, IQuery<IEnumerable<string>>
{
    private readonly AppIoFinancialReportQuartersQuery _command = command;
    private static readonly string _sql = AppIoFinancialReportSQLBuilder.SelectQuarters();
    private static readonly string _orderBy = AppIoFinancialReportSQLBuilder.OrderByQuarters();

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
        var having = string.Empty;
        dynamic parameters = new ExpandoObject();

        // HAVING perché il builder termina con il GROUP BY sulla stessa colonna
        if (!string.IsNullOrEmpty(_command.Year))
        {
            having = " HAVING year_quarter LIKE @Year + '[_]%'";
            parameters.Year = _command.Year;
        }

        return await ((IDatabase)this).SelectAsync<string>(
           connection!,
           _sql + having + _orderBy,
           parameters,
           transaction);
    }
}
