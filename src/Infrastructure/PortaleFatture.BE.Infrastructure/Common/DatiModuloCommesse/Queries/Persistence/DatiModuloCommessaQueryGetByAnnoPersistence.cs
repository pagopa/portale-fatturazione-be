using System.Data;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaQueryGetByAnnoPersistence : DapperBase, IQuery<IEnumerable<DatiModuloCommessaTotale>?>
{ 
    private readonly int _annoValidita;
    private readonly string? _idEnte;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectByAnno();

    public DatiModuloCommessaQueryGetByAnnoPersistence(string? idEnte, int annoValidita)
    { 
        this._annoValidita = annoValidita; 
        this._idEnte = idEnte;
    }
    public async Task<IEnumerable<DatiModuloCommessaTotale>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        return await ((IDatabase)this).SelectAsync<DatiModuloCommessaTotale>(connection!, _sqlSelect.Add(schema),
             new
             {
                 idEnte = _idEnte, 
                 annoValidita = _annoValidita, 
             }, transaction);
    }
} 