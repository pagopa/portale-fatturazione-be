using System.Data;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;

public class RelTestataQueryGetByListaEntiPersistence(RelTestataQueryGetByListaEnti command) : DapperBase, IQuery<RelTestataDto?>
{
    private readonly RelTestataQueryGetByListaEnti _command = command;
    private static readonly string _sqlSelectAll = RelTestataSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = RelTestataSQLBuilder.SelectAllCount();
    private static readonly string _offSet = RelTestataSQLBuilder.OffSet();
    private static readonly string _orderBy = RelTestataSQLBuilder.OrderBy();
    public async Task<RelTestataDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var rel = new RelTestataDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        if (!_command.EntiIds.IsNullOrEmpty())
            where += $" WHERE internal_organization_id IN @entiIds";
        else
        {
            _command.EntiIds = null;
            where += $" WHERE 1=1 ";
        }
        var caricata = _command.Caricata;
        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura != null ? _command.TipologiaFattura : null;
        var idContratto = _command.IdContratto ?? null;

        if (!string.IsNullOrEmpty(tipoFattura))
            where += " AND TipologiaFattura=@TipologiaFattura";

        if (anno.HasValue)
            where += " AND year=@anno";
        if (mese.HasValue)
            where += " AND month=@mese";

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

        if (!string.IsNullOrEmpty(tipoFattura))
            query.TipologiaFattura = tipoFattura;

        if (!string.IsNullOrEmpty(idContratto))
            query.IdContratto = idContratto;

        if (!_command.EntiIds.IsNullOrEmpty())
            query.EntiIds = _command.EntiIds;

        var values = await ((IDatabase)this).QueryMultipleAsync<SimpleRelTestata>(
            connection!,
            sql,
            query,
            transaction);

        rel.RelTestate = (await values.ReadAsync<SimpleRelTestata>()).ToList();
        rel.Count = await values.ReadFirstAsync<int>();
        return rel;
    }
}