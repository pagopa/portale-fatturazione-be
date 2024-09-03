using PortaleFatture.BE.Api.Modules.Messaggi.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries;

namespace PortaleFatture.BE.Api.Modules.Messaggi.Extensions;

public static class MessaggioExtensions
{
    public static DocumentiStorageKey Map(this MessaggioDto messaggio)
    {
       return new DocumentiStorageKey(messaggio.IdEnte, messaggio.IdUtente, messaggio.TipologiaDocumento, messaggio.DataInserimento.Year, messaggio.Hash);
    }

    public static MessaggiQueryGetByIdUtente Map(this MessaggioRicercaRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new MessaggiQueryGetByIdUtente(authInfo)
        {
            AnnoValidita = req.Anno, 
            MeseValidita = req.Mese,
            Page = page, 
            Size = pageSize,
            TipologiaDocumento = req.TipologiaDocumento, 
            Letto = req.Letto
        };
    }

    public static MessaggioUpdateStateCommand Map3(this MessaggioRicercaByIdRequest req, AuthenticationInfo authInfo)
    {
        return new MessaggioUpdateStateCommand(authInfo)
        {
            Stato =  TipologiaStatoMessaggio.Disabilitato,
            IdMessaggio = req.IdMessaggio 
        };
    }

    public static MessaggioQueryGetById Map(this MessaggioRicercaByIdRequest req, AuthenticationInfo authInfo)
    {
        return new MessaggioQueryGetById(authInfo)
        {
            IdMessaggio = req.IdMessaggio
        };
    }
    public static MessaggioReadCommand Map2(this MessaggioRicercaByIdRequest req, AuthenticationInfo authInfo)
    {
        return new MessaggioReadCommand(authInfo)
        {
            IdMessaggio = req.IdMessaggio
        };
    }
}