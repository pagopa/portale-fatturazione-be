using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;

public static class RegolareEsecuzioneExtensions
{
    public static RelTipologieFattureByIdEnte Map(this RELTipologiaFatturaInternalRequest req, AuthenticationInfo authInfo)
    {
        return new RelTipologieFattureByIdEnte(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
        };
    }

    public static RelTestataByIdRequest Map(this RELDownloadInternalRequest request)
    {
        return new RelTestataByIdRequest()
        {
            IdTestata = request.IdTestata 
        };
    }


    public static RELDownloadInternalRequest Map(this RELDownloadRequest request, Session session)
    {
        return new RELDownloadInternalRequest()
        {
            IdTestata = request.IdTestata, 
            TypeDocument = request.TipoDocumentoREL,
            Session = session
        };
    }


    public static RelTestataQueryGetByIdEnte Map(this RELTestataRicercaInternalRequest request, AuthenticationInfo authInfo, int page, int pageSize)
    {
        return new RelTestataQueryGetByIdEnte(authInfo)
        {
            Anno = request.Anno,
            Caricata = request.Caricata,
            IdContratto = request.IdContratto,
            Mese = request.Mese,
            TipologiaFattura = request.TipologiaFattura,
            Page = page,
            Size = pageSize,
            IdEnte = request.Session?.IdEnte,
        };
    }

    public static RELTestataRicercaInternalRequest Map(this RELTestataRicercaExternalRequest request, Session session)
    {
        return new RELTestataRicercaInternalRequest()
        {
            Anno = request.Anno,
            Caricata = null, //request.Caricata,
            Mese = request.Mese,
            TipologiaFattura = request.TipologiaFattura,
            Page = request.Page,
            PageSize = request.PageSize,
            Session = session
        };
    }
}
