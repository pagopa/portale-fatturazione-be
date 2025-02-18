using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public sealed class WhiteListFatturaEnteTipologiaFatturaQueryPersistence(WhiteListFatturaEnteTipologiaFatturaQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly WhiteListFatturaEnteTipologiaFatturaQuery _command = command;
    private static readonly string _sqlSelectAll = FattureQueryRicercaBuilder.SelectWhiteListTipologiaFattura();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(
       connection!, _sqlSelectAll, _command, transaction);
    }
}