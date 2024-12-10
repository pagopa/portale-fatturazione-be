using System.Data;
using System.Dynamic;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

public class NotificaQueryGetByListEntiPersistencev2(NotificaQueryGetByListaEntiv2 command) : DapperBase, IQuery<NotificaDto?>
{
    private readonly NotificaQueryGetByListaEntiv2 _command = command;
    private static readonly string _sqlSelectAll = NotificaSQLBuilder.SelectAll();
    private static readonly string _sqlSelectAllCount = NotificaSQLBuilder.SelectAllCount();
    private static readonly string _offSet = NotificaSQLBuilder.OffSet();
    private static readonly string _orderBy = NotificaSQLBuilder.OrderBy();

    public async Task<NotificaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var notifiche = new NotificaDto();

        var where = string.Empty;
        var page = _command.Page;
        var size = _command.Size;
        var anno = _command.AnnoValidita;
        var mese = _command.MeseValidita;

        if (anno.HasValue)
            where += " WHERE n.year=@anno";
        if (mese.HasValue)
            where += " AND n.month=@mese";

        if (!_command.EntiIds.IsNullNotAny())
            where += $" AND internal_organization_id IN @entiIds";

        if (!_command.Recapitisti.IsNullNotAny())
            where += $" AND Recapitista IN @Recapitisti";

        if (!_command.Consolidatori.IsNullNotAny())
            where += $" AND Consolidatore IN @Consolidatori";

        var prodotto = string.IsNullOrEmpty(_command.Prodotto) ? null : _command.Prodotto;
        var cap = string.IsNullOrEmpty(_command.Cap) ? null : _command.Cap;
        var profilo = string.IsNullOrEmpty(_command.Profilo) ? null : _command.Profilo;
        var tipoNotifica = _command.TipoNotifica != null ? _command.TipoNotifica : null;
        var contestazione = _command.StatoContestazione ?? null;
        var iun = string.IsNullOrEmpty(_command.Iun) ? null : _command.Iun;
        var recipientId = string.IsNullOrEmpty(_command.RecipientId) ? null : _command.RecipientId;

        if (!string.IsNullOrEmpty(iun))
            where += " AND n.iun=@iun";

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

        if (!contestazione.IsNullNotAny() && contestazione!.SequenceEqual([1]))
            where += " and t.FKIdFlagContestazione is NULL";
        else if (!contestazione.IsNullNotAny() && contestazione!.Contains(1))
            where += " and (t.FKIdFlagContestazione is NULL OR t.FKIdFlagContestazione IN @contestazione)";
        else if (!contestazione.IsNullNotAny())
            where += " and t.FKIdFlagContestazione IN @contestazione";
        else if (contestazione.IsNullNotAny())
            contestazione = null;

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;

        var sql = string.Join(";", sqlEnte, sqlCount);

        dynamic parameters = new ExpandoObject();
        var sqlParameters = new List<SqlParameter>();

        if (page.HasValue)
        {
            parameters.Page = page;
            sqlParameters.Add(new SqlParameter("@Page", page));
        }

        if (size.HasValue)
        {
            parameters.Size = size;
            sqlParameters.Add(new SqlParameter("@Size", size));
        }

        if (anno.HasValue)
        {
            parameters.Anno = anno.Value;
            sqlParameters.Add(new SqlParameter("@Anno", anno.Value));
        }

        if (mese.HasValue)
        {
            parameters.Mese = mese.Value;
            sqlParameters.Add(new SqlParameter("@Mese", mese.Value));
        }


        if (!string.IsNullOrEmpty(prodotto))
            parameters.Prodotto = prodotto;

        if (!string.IsNullOrEmpty(cap))
            parameters.Cap = cap;

        if (!string.IsNullOrEmpty(profilo))
            parameters.Profilo = profilo;

        if (!string.IsNullOrEmpty(tnot))
            parameters.TipoNotifica = tnot;

        if (contestazione != null)
            parameters.Contestazione = contestazione;

        if (!string.IsNullOrEmpty(iun))
            parameters.Iun = iun;

        if (!_command.EntiIds.IsNullNotAny())
            parameters.EntiIds = _command.EntiIds;

        if (!_command.Recapitisti.IsNullNotAny())
            parameters.Recapitisti = _command.Recapitisti;

        if (!_command.Consolidatori.IsNullNotAny())
            parameters.Consolidatori = _command.Consolidatori;

        if (!string.IsNullOrEmpty(recipientId))
            parameters.RecipientId = recipientId;

        var notificas = new List<SimpleNotificaDto>();
        var totalCount = 0;
        using (var cmd = ((SqlConnection)connection!).CreateCommand())
        {
            cmd.CommandTimeout = 320;
            cmd.CommandText = sql;
            cmd.Parameters.AddRange([.. sqlParameters]);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var notifica = new SimpleNotificaDto
                {
                    IdEnte = reader["IdEnte"] as string,
                    RagioneSociale = reader["RagioneSociale"] as string,
                    Profilo = reader["Profilo"] as string,
                    IdContratto = reader["IdContratto"] as string,
                    CodiceFiscale = reader["CodiceFiscale"] as string,
                    PIva = reader["PIva"] as string,
                    CAP = reader["CAP"] as string,
                    StatoEstero = reader["StatoEstero"] as string,
                    NumberOfPages = reader["NumberOfPages"].ToString(),
                    GEnvelopeWeight = reader["GEnvelopeWeight"] as string,
                    CostEuroInCentesimi = reader["CostEuroInCentesimi"].ToString(),
                    TimelineCategory = reader["TimelineCategory"] as string,
                    Contestazione = reader["Contestazione"] as string,
                    StatoContestazione = reader.GetByte(reader.GetOrdinal("StatoContestazione")),
                    TipoNotifica = reader["TipoNotifica"] as string,
                    IdNotifica = reader["IdNotifica"] as string,
                    IUN = reader["IUN"] as string,
                    Consolidatore = reader["Consolidatore"] as string,
                    Recapitista = reader["Recapitista"] as string,
                    DataInvio = reader["DataInvio"] as string,
                    Data = reader["Data"] as string,
                    RecipientIndex = reader["RecipientIndex"] as string,
                    RecipientType = reader["RecipientType"] as string,
                    RecipientId = reader["RecipientId"] as string,
                    Anno = reader["Anno"].ToString(),
                    Mese = reader["Mese"].ToString(),
                    AnnoMeseGiorno = reader["AnnoMeseGiorno"] as string,
                    ItemCode = reader["ItemCode"] as string,
                    NotificationRequestId = reader["NotificationRequestId"] as string,
                    RecipientTaxId = reader["RecipientTaxId"] as string,
                    Fatturata = reader["Fatturata"] != DBNull.Value ? (bool?)reader["Fatturata"] : null,
                    Onere = reader["Onere"] as string,
                    NoteEnte = reader["NoteEnte"] as string,
                    RispostaEnte = reader["RispostaEnte"] as string,
                    NoteSend = reader["NoteSend"] as string,
                    NoteRecapitista = reader["NoteRecapitista"] as string,
                    NoteConsolidatore = reader["NoteConsolidatore"] as string,
                    TipoContestazione = reader["TipoContestazione"] as string,
                    TipologiaFattura = reader["TipologiaFattura"] as string
                };

                notificas.Add(notifica);
            }

            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32(0);
            }
        }
        notifiche.Notifiche = notificas;
        notifiche.Count = totalCount;
        return notifiche;
    }
}