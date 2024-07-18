using System.Data;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class TipoContestazioneQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<TipoContestazione>>
{
    private static readonly string _sqlSelect = TipoContestazioneSQLBuilder.SelectAll();

    public async Task<IEnumerable<TipoContestazione>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<TipoContestazione>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}