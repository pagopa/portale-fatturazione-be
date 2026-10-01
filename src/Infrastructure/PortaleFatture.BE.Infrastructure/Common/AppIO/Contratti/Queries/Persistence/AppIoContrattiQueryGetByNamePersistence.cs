using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;

/// <summary>
/// Esegue la ricerca dei contratti APP IO per nome dell'ente su [be].[vwAppioContracts].
/// </summary>
/// <param name="command">La query con il nome e i trimestri.</param>
public sealed class AppIoContrattiQueryGetByNamePersistence(AppIoContrattiQueryGetByName command) : DapperBase, IQuery<IEnumerable<AppIoContrattoNome>>
{
    private readonly AppIoContrattiQueryGetByName _command = command;
    private static readonly string _sql = AppIoContrattiSQLBuilder.SelectContractsId();
    private static readonly string _orderBy = AppIoContrattiSQLBuilder.OrderByName();

    /// <summary>
    /// Cerca il nome per sottostringa (LIKE) nei trimestri richiesti, o nel più recente, e ordina
    /// per nome. Il valore resta un parametro, ma i caratteri jolly del LIKE ('%', '_', '[') non
    /// vengono neutralizzati, come per i PSP.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>I contratti il cui nome contiene il testo cercato.</returns>
    public async Task<IEnumerable<AppIoContrattoNome>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();

        // nome assente = nessun filtro sul nome (LIKE '%%'), come per i PSP
        var where = " WHERE name LIKE '%' + @Name + '%'";
        parameters.Name = _command.Name ?? string.Empty;

        // senza trimestri richiesti si cerca nel più recente
        if (_command.YearQuarter.IsNullNotAny())
            where += " AND year_quarter = (SELECT MAX(year_quarter) FROM [be].[vwAppioContracts])";
        else
        {
            where += " AND year_quarter IN @YearQuarter";
            parameters.YearQuarter = _command.YearQuarter;
        }

        return await ((IDatabase)this).SelectAsync<AppIoContrattoNome>(
           connection!,
           _sql + where + _orderBy,
           parameters,
           transaction);
    }
}
