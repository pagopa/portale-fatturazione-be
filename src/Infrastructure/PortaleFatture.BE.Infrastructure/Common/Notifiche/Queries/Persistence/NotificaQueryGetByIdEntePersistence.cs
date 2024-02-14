﻿using System.Data;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;

public class NotificaQueryGetByIdEntePersistence(NotificaQueryGetByIdEnte command) : DapperBase, IQuery<NotificaDto?>
{
    private readonly NotificaQueryGetByIdEnte _command = command;
    private static readonly string _sqlSelectAll = NotificaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = NotificaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = NotificaSQLBuilder.OffSet();
    private static readonly string _orderBy = NotificaSQLBuilder.OrderBy();
    public async Task<NotificaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var notifiche = new NotificaDto();
        var where = string.Empty;
        var idEnte = _command.AuthenticationInfo.IdEnte;
        var page = _command.Page;
        var size = _command.Size;
        where += " WHERE internal_organization_id=@IdEnte ";


        var anno = _command.AnnoValidita;
        var mese = _command.MeseValidita;
        var prodotto = string.IsNullOrEmpty(_command.Prodotto) ? null : _command.Prodotto;
        var cap = string.IsNullOrEmpty(_command.Cap) ? null : _command.Cap;
        var profilo = string.IsNullOrEmpty(_command.Profilo) ? null : _command.Profilo;
        var tipoNotifica = _command.TipoNotifica != null ? _command.TipoNotifica : null;
        int? contestazione = _command.StatoContestazione != null ? (int)_command.StatoContestazione : null;
        var iun = string.IsNullOrEmpty(_command.Iun) ? null : _command.Iun;

        if (!string.IsNullOrEmpty(iun))
            where += " AND iun=@iun";

        if (anno.HasValue)
            where += " AND n.year=@anno";
        if (mese.HasValue)
            where += " AND n.month=@mese";
        if (!string.IsNullOrEmpty(prodotto))
            where += " AND c.product=@prodotto";
        if (!string.IsNullOrEmpty(cap))
            where += " AND zip_code=@cap";
        if (!string.IsNullOrEmpty(profilo))
            where += " AND e.institutionType=@profilo";
        var tnot = tipoNotifica.Map();
        if (tnot != null)
        {
            if (string.IsNullOrEmpty(tnot))
                where += " AND paper_product_type is NULL";
            else
                where += " AND paper_product_type=@TipoNotifica";
        }

        if (contestazione.HasValue)
            where += " and t.FKIdFlagContestazione=@contestazione";

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;
        var sql = String.Join(";", sqlEnte, sqlCount);

        var values = await ((IDatabase)this).QueryMultipleAsync<NotificaDto>(
            connection!,
            sql,
                        new QueryDto
                        {
                            IdEnte = idEnte,
                            Size = size,
                            Page = page,
                            Anno = anno,
                            Mese = mese,
                            Prodotto = prodotto,
                            Cap = cap,
                            Profilo = profilo,
                            TipoNotifica = tnot,
                            Contestazione = contestazione,
                            Iun = iun, 
                        }, transaction);

        notifiche.Notifiche = await values.ReadAsync<SimpleNotificaDto>();
        notifiche.Count = await values.ReadFirstAsync<int>();
        return notifiche;
    }
}