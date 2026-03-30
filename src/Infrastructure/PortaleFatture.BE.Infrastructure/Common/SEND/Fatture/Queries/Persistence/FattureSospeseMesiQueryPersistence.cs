using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureSospeseMesiQueryPersistence(FattureSospeseMesiQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureSospeseMesiQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectMesiSospese();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByMonth();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            _command.Anno
        };

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, parameters, transaction);
    }
}
