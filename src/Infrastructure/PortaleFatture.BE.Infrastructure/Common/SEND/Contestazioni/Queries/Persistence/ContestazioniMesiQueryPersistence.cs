using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniMesiQueryPersistence(ContestazioniMesiQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly ContestazioniMesiQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectMesi();
    private static readonly string _orderBy = ContestazioniMassiveSQLBuilder.OrderByMonth();
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