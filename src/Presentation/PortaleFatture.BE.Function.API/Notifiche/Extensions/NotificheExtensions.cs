using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Function.API.Notifiche.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.Function.API.Notifiche.Extensions;

public static class NotificheExtensions
{
    public static IEnumerable<TipoNotificaResponse> GetAllTipologiaNotifica()
    {
           return Enum.GetValues(typeof(TipoNotifica))
                    .Cast<TipoNotifica>()
                    .Select(e => new TipoNotificaResponse
                    {
                        TipoNotifica = (int)e,
                        Descrizione = e.ToString()
                    }); 
    }

    public static NotificaQueryGetByIdEnte Map(this NotificheRicercaRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByIdEnte(authInfo)
        {
            AnnoValidita = req.Anno,
            Cap = req.Cap,
            MeseValidita = req.Mese,
            Page = page,
            Prodotto = req.Prodotto,
            Profilo = req.Profilo,
            Size = pageSize,
            TipoNotifica = req.TipoNotifica,
            StatoContestazione = req.StatoContestazione,
            Iun = req.Iun,
            RecipientId = req.RecipientId
        };
    }

    public static NotificheRicercaInternalRequest Map(this NotificheRicercaRequest request, Session session)
    {
        return new NotificheRicercaInternalRequest
        {
            Anno = request.Anno,
            Mese = request.Mese,
            Prodotto = request.Prodotto,
            Cap = request.Cap,
            Profilo = request.Profilo,
            TipoNotifica = request.TipoNotifica,
            StatoContestazione = request.StatoContestazione,
            Iun = request.Iun,
            RecipientId = request.RecipientId,
            IdEnte = request.IdEnte,
            RagioneSociale = request.RagioneSociale,
            IdContratto = request.IdContratto,
            IdReport = request.IdReport,
            Session = session
        };
    } 
    public static NotificheRicercaRequest Mapv2(this NotificheRicercaInternalRequest request)
    {
        return new NotificheRicercaRequest
        {
            Anno = request.Anno,
            Mese = request.Mese,
            Prodotto = request.Prodotto,
            Cap = request.Cap,
            Profilo = request.Profilo,
            TipoNotifica = request.TipoNotifica,
            StatoContestazione = request.StatoContestazione,
            Iun = request.Iun,
            RecipientId = request.RecipientId,
            IdEnte = request.IdEnte,
            RagioneSociale = request.RagioneSociale,
            IdContratto = request.IdContratto,
            IdReport = request.IdReport, 
        };
    }
}  