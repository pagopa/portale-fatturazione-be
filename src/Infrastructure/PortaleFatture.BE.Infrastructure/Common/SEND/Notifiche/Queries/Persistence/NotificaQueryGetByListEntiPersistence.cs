using System.Data;
using System.Dynamic;
using Dapper;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;
//perchè abbiamo bisogno di questa riga ?
using static PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder.NotificaSQLBuilder;
namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;
public class NotificaQueryGetByListEntiPersistence(NotificaQueryGetByListaEnti command) : DapperBase, IQuery<NotificaDto?>
{
    private readonly NotificaQueryGetByListaEnti _command = command;
    private static readonly string _sqlSelectAll = NotificaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = NotificaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = NotificaSQLBuilder.OffSet();

    public async Task<NotificaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var _orderBy = NotificaSQLBuilder.OrderBy(new SortParamSQLBuilder(_command.ColumName, _command.OrderDir));
        var notifiche = new NotificaDto();
        var page = _command.Page;
        var size = _command.Size;

        // WHERE e parametri sono composti da NotificaFiltriSQLBuilder, condiviso con la gemella v2
        // (dove il frammento era duplicato per copia). L'invariante "ogni @segnaposto del WHERE ha il
        // suo parametro" e' verificata a unit in NotificaFiltriBuilderUnitTests.
        var filtri = NotificaFiltriSQLBuilder.Componi(NotificaFiltriInput.Da(_command));
        var where = filtri.Where;

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;

        var sql = string.Join(";", sqlEnte, sqlCount);

        // Resta un ExpandoObject perche' e' quello che Dapper riceve oggi: cambiarne il tipo
        // cambierebbe il modo in cui espande i parametri delle liste degli IN.
        var parameters = new ExpandoObject();
        var valori = (IDictionary<string, object?>)parameters;
        foreach (var parametro in filtri.Parametri)
            valori[parametro.Key] = parametro.Value;

        using (var values = await ((IDatabase)this).QueryMultipleAsync<SimpleNotificaDto>(
            connection!,
            sql,
            parameters,
            transaction,
            CommandType.Text,
            320,
            CommandFlags.NoCache))
        {
            notifiche.Notifiche = await values.ReadAsync<SimpleNotificaDto>();
            notifiche.Count = await values.ReadFirstAsync<int>();
        }

        return notifiche;
    }
}