using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
 

public class DatiValoriRegioneModuloCommessaQueryGetPersistence(string? idEnte, int anno, int mese) : DapperBase, IQuery<IEnumerable<ValoriRegioneDto>?>
{ 
    private readonly int _anno = anno;
    private readonly int _mese = mese;
    private readonly string? _idEnte = idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectValoriRegioni();

    public async Task<IEnumerable<ValoriRegioneDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<ValoriRegioneDto>(connection!, _sqlSelect.Add(schema),
            new
            {
                idEnte = _idEnte,
                mese = _mese,
                anno = _anno, 
            }, transaction);
    }
}