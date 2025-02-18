using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class WhiteListFatturaEnteAnniQueryPersistence(WhiteListFatturaEnteAnniQuery command) : DapperBase, IQuery<IEnumerable<int>?>
{
    private readonly WhiteListFatturaEnteAnniQuery _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectWhiteListAnni();
    public async Task<IEnumerable<int>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<int>(
       connection!, _sqlSelectAll, _command, transaction);
    }
}