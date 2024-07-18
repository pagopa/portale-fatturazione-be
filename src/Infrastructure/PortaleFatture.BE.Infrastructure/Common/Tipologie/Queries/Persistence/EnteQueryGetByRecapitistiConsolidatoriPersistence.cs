using System.Data;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class EnteQueryGetByRecapitistiConsolidatoriPersistence(string? tipo) : DapperBase, IQuery<IEnumerable<Ente>>
{
    private static readonly string _sqlSelect = EnteSQLBuilder.SelectAllByTipo();
    private readonly string? _tipo = tipo!; 

    public async Task<IEnumerable<Ente>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var sql = _sqlSelect.Add(schema);
  
        return await ((IDatabase)this).SelectAsync<Ente>(connection!, sql, new 
        {
           profilo = _tipo
        }, transaction);
    }
}