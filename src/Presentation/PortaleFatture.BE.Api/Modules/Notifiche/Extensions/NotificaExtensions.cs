using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;

namespace PortaleFatture.BE.Api.Modules.Notifiche.Extensions;

public static class NotificaExtensions
{
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
            Iun = req.Iun
        };
    } 
    public static NotificaQueryGetByListaEnti Map(this NotificheRicercaRequestPagoPA req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByListaEnti(authInfo)
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
            EntiIds = req.IdEnti
        };
    }
}