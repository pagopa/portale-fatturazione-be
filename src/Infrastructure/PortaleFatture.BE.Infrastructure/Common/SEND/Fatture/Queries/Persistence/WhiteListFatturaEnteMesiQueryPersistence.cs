using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class WhiteListFatturaEnteMesiQueryPersistence(WhiteListFatturaEnteMesiQuery command) : DapperBase, IQuery<IEnumerable<int>?>
{
    private readonly WhiteListFatturaEnteMesiQuery _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectWhiteListMesi();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByWhiteListMesi(); 
    public async Task<IEnumerable<int>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var where  = " WHERE anno=@anno AND DataFine IS NULL"; 
        return await ((IDatabase)this).SelectAsync<int>(connection!, _sqlSelectAll + where + _orderBy, _command, transaction);
    }
}