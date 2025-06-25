using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

public class TipologiaReportQueryPersistence(TipologiaReportQuery command) : DapperBase, IQuery<IEnumerable<TipologiaReport>?>
{
    private readonly TipologiaReportQuery _command = command;
    private static readonly string _sql = ContestazioniMassiveSQLBuilder.SelectTipologiaReport(); 
    public async Task<IEnumerable<TipologiaReport>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        dynamic parameters = new ExpandoObject();
        var where = " WHERE Attivo=1 AND CategoriaDocumento like '%contestaz%' "; 
  
        return await ((IDatabase)this).SelectAsync<TipologiaReport>(
            connection!, _sql + where + " ORDER BY IdTipologiaReport", parameters, transaction); 
    }
}