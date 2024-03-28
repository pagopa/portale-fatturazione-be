using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

namespace PortaleFatture.BE.Api.Modules.DatiRel.Extensions;

public static class RelExtensions
{
    public static RelUploadGetById Map(this RelUploadByIdRequest req, AuthenticationInfo authInfo)
    {
        return new RelUploadGetById(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdContratto = req.IdContratto,
            TipologiaFattura = req.TipologiaFattura 
        };
    }

    public static RelTestataQueryGetByListaEnti Map(this RelTestataRicercaRequestPagoPA req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new RelTestataQueryGetByListaEnti(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdContratto = req.IdContratto,
            TipologiaFattura = req.TipologiaFattura,
            Page = page,
            Size = pageSize,
            EntiIds = req.IdEnti,
            Caricata = req.Caricata
        };
    }

    public static RelTestataQueryGetByIdEnte Map(this RelTestataRicercaRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new RelTestataQueryGetByIdEnte(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdContratto = req.IdContratto,
            TipologiaFattura = req.TipologiaFattura, 
            Caricata = req.Caricata,
            Page = page,
            Size = pageSize
        };
    }

    public static RelTestataQueryGetById Map(this RelTestataByIdRequest req, AuthenticationInfo authInfo)
    {
        return new RelTestataQueryGetById(authInfo)
        {
            IdTestata = req.IdTestata
        };
    }

    public static RelRigheQueryGetById Map(this RelRigheByIdRequest req, AuthenticationInfo authInfo)
    {
        return new RelRigheQueryGetById(authInfo)
        {
            IdTestata = req.IdTestata
        };
    }

    public static RelDocumentoDto Map(this RelTestataDettaglioDto req)
    {
        return new RelDocumentoDto()
        {
            Mese = Convert.ToInt32(req.Mese).GetMonth(),
            Anno = req.Anno,
            Totale = req.Totale.ToString(),
            TotaleAnalogico = req.TotaleAnalogico.ToString(),
            TotaleDigitale = req.TotaleDigitale.ToString(),
            TotaleNotificheAnalogiche = req.TotaleNotificheAnalogiche,
            TotaleNotificheDigitali = req.TotaleNotificheDigitali,
            RagioneSociale = req.RagioneSociale,
            IdContratto = req.IdContratto
        };
    }
}