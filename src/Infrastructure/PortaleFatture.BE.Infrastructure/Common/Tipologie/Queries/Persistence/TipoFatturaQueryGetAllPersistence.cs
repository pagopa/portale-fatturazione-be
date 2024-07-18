using System.Data;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class TipoFatturaQueryGetAllPersistence(int anno, int mese) : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelect = TipoFatturaSQLBuilder.SelectAll();
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelect.Add(schema), new { 
          anno = _anno,
          mese = _mese 
        }, transaction);
    }
}