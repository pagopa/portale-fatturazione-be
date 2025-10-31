using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class DatiModuloCommessaPrevisionaleQueryGetByAnnoPersistence(string? idEnte, int? anno, int? mese, string? prodotto) : DapperBase, IQuery<IEnumerable<ModuloCommessaPrevisionaleByAnnoDto>?>
{
    private readonly int? _anno = anno;
    private readonly string? _idEnte = idEnte;
    private readonly string? _prodotto = prodotto;
    private readonly int? _mese = mese;
    private static readonly string _sqlSelect = DatiModuloCommessaTotaleSQLBuilder.SelectPrevisionaleByAnnoAndIdEnte();

    public async Task<IEnumerable<ModuloCommessaPrevisionaleByAnnoDto>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    { 
        var sql = _sqlSelect;

        return await ((IDatabase)this).SelectAsync<ModuloCommessaPrevisionaleByAnnoDto>(connection!, sql,
                 new
                 {
                     idEnte = _idEnte,
                     annoFiltro = _anno,
                     prodotto = _prodotto, 
                     mese = _mese
                 }, transaction);
    }
}