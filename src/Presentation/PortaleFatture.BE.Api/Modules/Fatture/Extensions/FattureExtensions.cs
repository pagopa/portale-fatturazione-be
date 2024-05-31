using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;

namespace PortaleFatture.BE.Api.Modules.Fatture.Extensions;

public static class FattureExtensions
{
    public static FattureQueryRicerca Map(this FatturaRicercaRequest req, AuthenticationInfo authInfo)
    {
        return new FattureQueryRicerca(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti,
            TipologiaFattura = req.TipologiaFattura
        };
    }
} 