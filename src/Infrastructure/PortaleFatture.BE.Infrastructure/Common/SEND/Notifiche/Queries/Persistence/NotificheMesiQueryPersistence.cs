using System.Data;
using System.Dynamic;
using global::PortaleFatture.BE.Infrastructure.Common.Persistence;
using global::PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class NotificheMesiQueryPersistence(NotificheMesiQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly NotificheMesiQuery _command = command;
    private static readonly string _sql =  NotificaSQLBuilder.SelectMesi();
    private static readonly string _orderBy = NotificaSQLBuilder.OrderByMonth();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " WHERE year=@anno ";
            parameters.Anno = _command.Anno;
        }

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + where + _orderBy, parameters, transaction);
    }
}