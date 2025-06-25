using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class ContestazioniAnniQueryPersistence(ContestazioniAnniQuery command) : DapperBase, IQuery<IEnumerable<string>?>
{
    private readonly ContestazioniAnniQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectAnni();
    private static readonly string _orderBy = ContestazioniMassiveSQLBuilder.OrderByYear();
    public async Task<IEnumerable<string>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {

        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, _command, transaction); 
    }
}