using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;


namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public class NotificheAnniQueryPersistence(NotificheAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly NotificheAnniQuery _command = command;
    private static readonly string _sql = NotificaSQLBuilder.SelectAnni();
    private static readonly string _orderBy = NotificaSQLBuilder.OrderByYear();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, _command, transaction);
    }
}