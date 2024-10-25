using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence;

public class CalendarioContestazioneQueryGetAllPersistence(CalendarioContestazioneQueryGetAll command) : DapperBase, IQuery<IEnumerable<CalendarioContestazione>?>
{
    private readonly CalendarioContestazioneQueryGetAll _command = command;
    private static readonly string _sql = CalendarioContestazioneSQLBuilder.SelectAll();
    private static readonly string _orderBy = CalendarioContestazioneSQLBuilder.OrderBy();
    public async Task<IEnumerable<CalendarioContestazione>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sql + _orderBy;
        return await ((IDatabase)this).SelectAsync<CalendarioContestazione>(connection!, sql.Add(schema), transaction);
    }
}