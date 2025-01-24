using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

public class AccertamentiAnniQueryPersistence(AccertamentiAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly AccertamentiAnniQuery _command = command;
    private static readonly string _sql = ReportSQLBuilder.SelectAnni();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql, _command, transaction);
    }
}