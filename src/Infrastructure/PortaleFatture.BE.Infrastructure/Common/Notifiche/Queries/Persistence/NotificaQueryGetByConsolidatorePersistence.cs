using System.Data;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence;
public class NotificaQueryGetByConsolidatorePersistence(NotificaQueryGetByConsolidatore command) : DapperBase, IQuery<NotificaRECCONDto?>
{
    private readonly NotificaQueryGetByConsolidatore _command = command;
    private static readonly string _sqlSelectAll = NotificaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = NotificaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = NotificaSQLBuilder.OffSet();
    private static readonly string _orderBy = NotificaSQLBuilder.OrderBy();

    public async Task<NotificaRECCONDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var notifiche = new NotificaRECCONDto();
        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        var anno = _command.AnnoValidita;
        var mese = _command.MeseValidita;

        if (anno.HasValue)
            where += " WHERE n.year=@anno";
        if (mese.HasValue)
            where += " AND n.month=@mese";

        where += $" AND Consolidatore = @consolidatore ";

        var consolidatore = _command.AuthenticationInfo.IdEnte; 
   
        var prodotto = string.IsNullOrEmpty(_command.Prodotto) ? null : _command.Prodotto;
        var cap = string.IsNullOrEmpty(_command.Cap) ? null : _command.Cap;
        var profilo = string.IsNullOrEmpty(_command.Profilo) ? null : _command.Profilo;
        var tipoNotifica = _command.TipoNotifica != null ? _command.TipoNotifica : null;
        var contestazione = _command.StatoContestazione ?? null;
        var iun = string.IsNullOrEmpty(_command.Iun) ? null : _command.Iun;
        var recipientId = string.IsNullOrEmpty(_command.RecipientId) ? null : _command.RecipientId;

        if (!string.IsNullOrEmpty(iun))
            where += " AND iun=@iun";

        if (!string.IsNullOrEmpty(recipientId))
            where += " AND recipient_id=@recipientId";


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

        if (!contestazione.IsNullNotAny() && Enumerable.SequenceEqual(contestazione!, [1]))
            where += " and t.FKIdFlagContestazione is NULL";
        else if (!contestazione.IsNullNotAny() && contestazione!.Contains(1))
            where += " and (t.FKIdFlagContestazione is NULL OR t.FKIdFlagContestazione IN @contestazione)";
        else if (!contestazione.IsNullNotAny())
            where += " and t.FKIdFlagContestazione IN @contestazione";

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;
        var sql = String.Join(";", sqlEnte, sqlCount);

        var query = new QueryDto
        {
            Size = size,
            Page = page,
            Anno = anno,
            Mese = mese,
            Consolidatore = consolidatore
        };

        if(!string.IsNullOrEmpty(prodotto)) 
            query.Prodotto = prodotto;

        if (!string.IsNullOrEmpty(cap))
            query.Cap = cap;

        if (!string.IsNullOrEmpty(profilo))
            query.Profilo = profilo;

        if (!string.IsNullOrEmpty(tnot))
            query.TipoNotifica = tnot;

        if (contestazione != null)
            query.Contestazione = contestazione;

        if (!string.IsNullOrEmpty(iun))
            query.Iun = iun; 

        if (!string.IsNullOrEmpty(recipientId))
            query.RecipientId = recipientId;

        using var values = await ((IDatabase)this).QueryMultipleAsync<RECCONNotificaDto>(
            connection!,
            sql,
            query,
            transaction);
        notifiche.Notifiche = await values.ReadAsync<RECCONNotificaDto>();
        notifiche.Count = await values.ReadFirstAsync<int>();
        return notifiche;
    }
}