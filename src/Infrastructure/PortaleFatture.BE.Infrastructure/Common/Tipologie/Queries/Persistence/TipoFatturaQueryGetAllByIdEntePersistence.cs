using System.Data;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

public class TipoFatturaQueryGetAllByIdEntePersistence(string? idEnte, int anno, int mese) : DapperBase, IQuery<IEnumerable<string>>
{
    private static readonly string _sqlSelect = TipoFatturaSQLBuilder.SelectAllByIdEnte();
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    private readonly string? _idEnte = idEnte;
    public async Task<IEnumerable<string>> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    { 
        return await ((IDatabase)this).SelectAsync<string>(connection!, _sqlSelect.Add(schema), new { 
          anno = _anno,
          mese = _mese,
          idEnte = _idEnte
        }, transaction);
    }
}