using System.Data;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries.Persistence;

public class CalendarioContestazioneQueryGetAllPersistence(CalendarioContestazioneQueryGetAll command) : DapperBase, IQuery<IEnumerable<CalendarioContestazione>?>
{
    private readonly CalendarioContestazioneQueryGetAll _command = command;
    private static readonly string _sql = CalendarioContestazioneSQLBuilder.SelectAll();
    public async Task<IEnumerable<CalendarioContestazione>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<CalendarioContestazione>(connection!, _sql.Add(schema), transaction);
    }
}