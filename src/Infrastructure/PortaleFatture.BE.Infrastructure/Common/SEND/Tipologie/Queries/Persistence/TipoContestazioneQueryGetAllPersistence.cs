using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class TipoContestazioneQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<TipoContestazione>>
{
    private static readonly string _sqlSelect = TipoContestazioneSQLBuilder.SelectAll();

    public async Task<IEnumerable<TipoContestazione>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);
        sql += "   where FlagContestazione = 'True' ";
        return await ((IDatabase)this).SelectAsync<TipoContestazione>(connection!, sql, null, transaction);
    }
}