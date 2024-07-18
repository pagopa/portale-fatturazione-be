using System.Data;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class StatoCommessaQueryGetByDefaultPersistence : DapperBase, IQuery<StatoCommessa?>
{
    private static readonly string _sqlSelect = StatoCommessaSQLBuilder.SelectBy();
    public async Task<StatoCommessa?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var results = await ((IDatabase)this).SelectAsync<StatoCommessa>(connection!, _sqlSelect.Add(schema), null, transaction);
        return results.IsNullNotAny() ? null : results.FirstOrDefault();
    }
}