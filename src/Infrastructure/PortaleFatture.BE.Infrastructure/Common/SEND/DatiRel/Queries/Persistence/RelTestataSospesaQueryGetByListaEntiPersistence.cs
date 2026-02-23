using System.Data;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

public class RelTestataSospesaQueryGetByListaEntiPersistence(RelTestataSospesaQueryGetByListaEnti command) : DapperBase, IQuery<RelTestataSospesaDto?>
{
    private readonly RelTestataSospesaQueryGetByListaEnti _command = command;
    private static readonly string _sqlSelectAll = RelTestataSospesaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = RelTestataSospesaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = RelTestataSospesaSQLBuilder.OffSet();
    private static readonly string _orderBy = RelTestataSospesaSQLBuilder.OrderByPagoPA();
    public async Task<RelTestataSospesaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var rel = new RelTestataSospesaDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;


        var caricata = _command.Caricata;
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura != null ? _command.TipologiaFattura : null;
        var idContratto = _command.IdContratto ?? null;
        var idTipoContratto = _command.FkIdTipoContratto;

        if (anno.HasValue)
            where += " WHERE t.year=@anno";
        if (mese.HasValue)
            where += " AND t.month=@mese";

        if (!_command.EntiIds.IsNullNotAny())
            where += $" AND internal_organization_id IN @entiIds";  

        if (idTipoContratto.HasValue)
            where += " AND c.FkIdTipoContratto = @FkIdTipoContratto";

        if (!string.IsNullOrEmpty(tipoFattura))
            where += " AND TipologiaFattura=@TipologiaFattura";

        if (!string.IsNullOrEmpty(idContratto))
            where += " AND contract_id=@IdContratto";

        if (caricata.HasValue)
            where += " AND caricata=@caricata";

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
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

        if (idTipoContratto.HasValue)
            query.FkIdTipoContratto = idTipoContratto;

        if (!string.IsNullOrEmpty(tipoFattura))
            query.TipologiaFattura = tipoFattura;

        if (!string.IsNullOrEmpty(idContratto))
            query.IdContratto = idContratto;

        if (!_command.EntiIds.IsNullNotAny())
            query.EntiIds = _command.EntiIds;

        using var values = await ((IDatabase)this).QueryMultipleAsync<SimpleRelTestata>(
            connection!,
            sql,
            query,
            transaction);
        rel.RelTestate = [.. (await values.ReadAsync<SimpleRelTestata>())];
        rel.Count = await values.ReadFirstAsync<int>();
        return rel;
    }
}