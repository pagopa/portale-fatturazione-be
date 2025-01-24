using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureMesiQueryPersistence(FattureMesiQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly FattureMesiQuery _command = command;
    private static readonly string _sql = FattureQueryRicercaBuilder.SelectMesi();
    private static readonly string _orderBy = FattureQueryRicercaBuilder.OrderByMonth();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " WHERE annoriferimento=@anno ";
            parameters.Anno = _command.Anno;
        }

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + where + _orderBy, parameters, transaction);
    }
}