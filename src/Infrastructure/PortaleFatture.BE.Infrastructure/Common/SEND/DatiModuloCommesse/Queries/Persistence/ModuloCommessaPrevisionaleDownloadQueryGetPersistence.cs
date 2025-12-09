using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

public class ModuloCommessaPrevisionaleDownloadQueryGetPersistence(ModuloCommessaPrevisionaleDownloadQueryGet command, CancellationToken ct = default) : DapperBase, IQuery<IEnumerable<ModuloCommessaPrevisionaleDownloadDtov2>?>
{
    private readonly string[]? _idEnti = command.IdEnti;
    private readonly int? _anno = command.Anno;
    private readonly int? _mese = command.Mese;
    private readonly int? _idTipoContratto = command.IdTipoContratto;

    private static readonly string _sqlSelect = DatiModuloCommessaPrevisionaliSQLBuilder.SelectPrevisionaleByAnnoMeseIdEnte();
    private static readonly string _orderBy = DatiModuloCommessaPrevisionaliSQLBuilder.OrderbyAnnoMeseIdTipoReportEnte();
    public async Task<IEnumerable<ModuloCommessaPrevisionaleDownloadDtov2>?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken ct = default)
    {
        string? where = string.Empty;
        if (!_idEnti.IsNullNotAny())
           where = " AND identi IN @idente";

        return await ((IDatabase)this).SelectAsync<ModuloCommessaPrevisionaleDownloadDtov2>(connection!, $"{_sqlSelect}{where}{_orderBy}",
              new
              {
                  anno = _anno,
                  mese = _mese,
                  idente = _idEnti,
                  idTipoContratto = _idTipoContratto
              }, transaction);
    }
}