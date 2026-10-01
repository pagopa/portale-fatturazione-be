using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;

/// <summary>
/// Esegue la ricerca dei contratti APP IO su [be].[vwAppioContracts]: righe della pagina e conteggio
/// totale in un'unica chiamata (QueryMultiple), con lo stesso filtro.
/// </summary>
/// <param name="command">La query con filtri e paginazione.</param>
public sealed class AppIoContrattiQueryGetByRicercaPersistence(AppIoContrattiQueryGetByRicerca command) : DapperBase, IQuery<AppIoContrattiListDto>
{
    private readonly AppIoContrattiQueryGetByRicerca _command = command;

    private static readonly string _sql = AppIoContrattiSQLBuilder.SelectAll();
    private static readonly string _sqlCount = AppIoContrattiSQLBuilder.SelectAllCount();
    private static readonly string _orderBy = AppIoContrattiSQLBuilder.OrderBy();
    private static readonly string _offSet = AppIoContrattiSQLBuilder.OffSet();

    /// <summary>
    /// Compone il filtro (trimestri, con il più recente come default, ed eventuali contratti), poi
    /// legge la pagina ordinata per contract_id e il conteggio totale.
    /// </summary>
    /// <param name="connection">La connessione al database.</param>
    /// <param name="schema">Lo schema del contesto (non usato: la vista è nello schema be).</param>
    /// <param name="transaction">La transazione corrente, se presente.</param>
    /// <param name="cancellationToken">Il token di annullamento.</param>
    /// <returns>I contratti della pagina e il conteggio totale.</returns>
    public async Task<AppIoContrattiListDto> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        List<string> where = [];
        dynamic parameters = new ExpandoObject();

        // la paginazione si applica solo se arrivano entrambi i parametri
        var offset = _offSet;
        if (_command.Page.HasValue && _command.Size.HasValue)
        {
            parameters.Page = _command.Page;
            parameters.Size = _command.Size;
        }
        else
            offset = string.Empty;

        // senza trimestri richiesti si mostra il più recente, come per i PSP
        if (_command.YearQuarter.IsNullNotAny())
            where.Add("year_quarter = (SELECT MAX(year_quarter) FROM [be].[vwAppioContracts])");
        else
        {
            where.Add("year_quarter IN @YearQuarter");
            parameters.YearQuarter = _command.YearQuarter;
        }

        if (!_command.ContractIds.IsNullNotAny())
        {
            where.Add("contract_id IN @ContractIds");
            parameters.ContractIds = _command.ContractIds;
        }

        var sWhere = " WHERE " + string.Join(" AND ", where);

        var sql = _sql + sWhere + _orderBy + offset;
        var sqlCount = _sqlCount + sWhere;

        using var values = await ((IDatabase)this).QueryMultipleAsync<AppIoContratto>(
            connection!,
            string.Join(";", sql, sqlCount),
            parameters,
            transaction,
            CommandType.Text,
            null,
            CommandFlags.NoCache);

        return new AppIoContrattiListDto
        {
            Contratti = await values.ReadAsync<AppIoContratto>(),
            Count = await values.ReadFirstAsync<int>()
        };
    }
}
