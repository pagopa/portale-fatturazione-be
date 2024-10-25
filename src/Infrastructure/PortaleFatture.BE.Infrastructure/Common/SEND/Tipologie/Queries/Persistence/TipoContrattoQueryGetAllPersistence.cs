using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class TipoContrattoQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<TipoContratto>>
{
    private static readonly string _sqlSelect = TipoContrattoSQLBuilder.SelectAll();

    public async Task<IEnumerable<TipoContratto>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<TipoContratto>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}