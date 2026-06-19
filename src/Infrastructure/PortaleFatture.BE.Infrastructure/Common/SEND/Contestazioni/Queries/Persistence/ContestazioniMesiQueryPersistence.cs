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

        if (!string.IsNullOrEmpty(_command.Anno))
        {
            parameters.Anno = _command.Anno;
        }
  
        var idEnte = _command.AuthenticationInfo.IdEnte;

        if (string.IsNullOrWhiteSpace(idEnte))
        {
            parameters.IdEnte = null;
        }

        if (!string.IsNullOrEmpty(idEnte))
        {
            parameters.IdEnte = idEnte;
        }


        return await ((IDatabase)this).SelectAsync<string>(
            connection!, _sql + _orderBy, parameters, transaction); 
    }
}