using System.Data;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

public class AccertamentiMesiQueryPersistence(AccertamentiMesiQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly AccertamentiMesiQuery _command = command;
    private static readonly string _sql = ReportSQLBuilder.SelectMesi();
    private static readonly string _orderBy = ReportSQLBuilder.OrderByMonth;
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = string.Empty;
        if (!string.IsNullOrEmpty(_command.Anno))
        {
            where += " WHERE anno=@anno ";
            parameters.Anno = _command.Anno;
        }

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + where + _orderBy, parameters, transaction);
    }
}