using System.Data; 
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;

public class RelTestataQueryGetByListaEntiQuadraturaPersistence(RelTestataQueryGetByListaEntiQuadratura command) : DapperBase, IQuery<RelTestataQuadraturaDto?>
{
    private readonly RelTestataQueryGetByListaEntiQuadratura _command = command;
    private static readonly string _sqlSelectAll = RelTestataQuadraturaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllNoTipologia = RelTestataQuadraturaSQLBuilder.SelectAllNoTipologia();
    private static readonly string _sqlSelectAllCount = RelTestataQuadraturaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = RelTestataQuadraturaSQLBuilder.OffSet();
    private static readonly string _orderBy = RelTestataQuadraturaSQLBuilder.OrderBy();
    public async Task<RelTestataQuadraturaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var rel = new RelTestataQuadraturaDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        var anno = _command.Anno;
        var mese = _command.Mese;

        if (anno.HasValue)
            where += " WHERE nc.year=@anno";
        if (mese.HasValue)
            where += " AND nc.month=@mese";

        if (!_command.EntiIds.IsNullNotAny())
            where += $" AND nc.internal_organization_id IN @entiIds";

        var caricata = _command.Caricata;

        var tipoFattura = _command.TipologiaFattura != null ? _command.TipologiaFattura : null;
        var idContratto = _command.IdContratto ?? null;


        if (!string.IsNullOrEmpty(idContratto))
            where += " AND nc.contract_id=@IdContratto";

        if (caricata.HasValue)
            where += " AND r.caricata=@caricata";

        var orderBy = _orderBy;

        string? sqlEnte;
        if (!string.IsNullOrEmpty(tipoFattura))
            sqlEnte = _sqlSelectAll;
        else
            sqlEnte = _sqlSelectAllNoTipologia;

        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;
        var sql = string.Join(";", sqlEnte, sqlCount);

        var query = new RelQueryDto
        {
            Size = size,
            Page = page,
            Anno = anno,
            Mese = mese,
            Caricata = caricata,
            EntiIds = _command.EntiIds
        };

        if (!string.IsNullOrEmpty(tipoFattura))
            query.TipologiaFattura = tipoFattura;

        if (!string.IsNullOrEmpty(idContratto))
            query.IdContratto = idContratto;

        if (!_command.EntiIds.IsNullNotAny())
            query.EntiIds = _command.EntiIds;

        var values = await ((IDatabase)this).QueryMultipleAsync<RelQuadraturaDto>(
            connection!,
            sql,
            query,
            transaction);

        rel.Quadratura = (await values.ReadAsync<RelQuadraturaDto>()).ToList();
        rel.Count = await values.ReadFirstAsync<int>();
        return rel;
    }
}