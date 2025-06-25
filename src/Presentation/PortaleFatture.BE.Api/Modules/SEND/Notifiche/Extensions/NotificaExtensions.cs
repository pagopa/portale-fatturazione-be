using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;

public static class NotificaExtensions
{
    public static NotificaQueryGetByListaEntiv2 Mapv2(this NotificheRicercaRequestPagoPA req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByListaEntiv2(authInfo)
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
            EntiIds = req.IdEnti,
            RecipientId = req.RecipientId,
            Consolidatori = req.Consolidatori,
            Recapitisti = req.Recapitisti
        };
    }

    public static async Task Download(this HttpContext context, byte[]? data, string mime, string filename)
    {
        var totalBytes = data!.Length;
        context.Response.ContentType = mime; 
        context.Response.Headers.TryAdd("Content-Disposition", $"attachment; filename={filename}");
        context.Response.Headers["Content-Length"] = totalBytes.ToString();

        const int bufferSize = 32 * 1024;
        var buffer = new byte[bufferSize];

        var bytesRemaining = totalBytes;
        var offset = 0;

        var flushCounter = 0;
        while (bytesRemaining > 0)
        { 
            var chunkSize = Math.Min(bufferSize, bytesRemaining); 
            Buffer.BlockCopy(data, offset, buffer, 0, chunkSize); 
            await context.Response.Body.WriteAsync(buffer, 0, chunkSize);

            flushCounter++;
            if (flushCounter % 10 == 0)  
            {
                await context.Response.Body.FlushAsync();
            }
             
            offset += chunkSize;
            bytesRemaining -= chunkSize;
        }
    }

    public static NotificaQueryGetByConsolidatore Map2(this NotificheRicercaRequestPagoPA req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByConsolidatore(authInfo)
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

    public static NotificaQueryGetByRecapitista Map2(this NotificheRicercaRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByRecapitista(authInfo)
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
            EntiIds = req.IdEnti,
            RecipientId = req.RecipientId,
            Consolidatori = req.Consolidatori,
            Recapitisti = req.Recapitisti
        };
    }
}