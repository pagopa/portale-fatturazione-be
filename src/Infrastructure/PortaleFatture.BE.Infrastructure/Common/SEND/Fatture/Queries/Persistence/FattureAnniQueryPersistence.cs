using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureAnniQueryPersistence(FattureAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureAnniQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectAnni();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByYear();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, _command, transaction);
    }
}