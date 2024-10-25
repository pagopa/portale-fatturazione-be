using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

public class ProfiloQueryGetAllPersistence : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelect = ProfiloSQLBuilder.SelectAll();

    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelect.Add(schema), null, transaction);
    }
}