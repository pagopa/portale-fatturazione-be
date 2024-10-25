using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class FlagContestazioneQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<FlagContestazione>>
{
    private static readonly string _sqlSelect = FlagContestazioneSQLBuilder.SelectAll();

    public async Task<IEnumerable<FlagContestazione>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<FlagContestazione>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}