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

        var page = _command.Page;
        var size = _command.Size;

        // Stesso builder della v1: il frammento di WHERE era duplicato per copia fra le due classi.
        var filtri = NotificaFiltriSQLBuilder.Componi(NotificaFiltriInput.Da(_command));
        var where = filtri.Where;

        var orderBy = _orderBy;

        var sqlEnte = _sqlSelectAll;
        var sqlCount = _sqlSelectAllCount;
        if (page == null && size == null)
            sqlEnte += where + orderBy;
        else
            sqlEnte += where + orderBy + _offSet;

        sqlCount += where;

        var sql = string.Join(";", sqlEnte, sqlCount);

        // ⚠️ DIFETTO NOTO, riprodotto qui tale e quale: al comando arrivano SOLO i quattro parametri
        // scalari (Page/Size/Anno/Mese), mentre `filtri.Parametri` ne contiene fino a quattordici.
        // Ogni altro filtro presente nel WHERE resta senza il suo parametro -> SqlException. Prima
        // dell'estrazione il difetto era invisibile: l'insieme completo veniva costruito in un
        // ExpandoObject che poi non leggeva piu' nessuno. V. NotificaFiltriBuilderUnitTests.
        var sqlParameters = NotificaFiltriSQLBuilder.NomiParametriComandoV2(filtri)
            .Select(nome => new SqlParameter($"@{nome}", filtri.Parametri[nome]))
            .ToList();

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