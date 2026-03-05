using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureRiepilogoAnniQueryPersistence(FattureRiepilogoAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureRiepilogoAnniQuery _command = command;
    private static readonly string _sql = FattureRiepilogoQueryRicercaBuilder.SelectAllAnni();
    private static readonly string _orderBy = FattureRiepilogoQueryRicercaBuilder.OrderByYear();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, _command, transaction);
    }
}
