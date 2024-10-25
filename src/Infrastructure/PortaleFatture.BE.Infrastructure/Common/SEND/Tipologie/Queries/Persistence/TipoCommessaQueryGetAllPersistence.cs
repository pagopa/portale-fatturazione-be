using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class TipoCommessaQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<TipoCommessa>>
{
    private static readonly string _sqlSelect = TipoCommessaSQLBuilder.SelectAll();

    public async Task<IEnumerable<TipoCommessa>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<TipoCommessa>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}