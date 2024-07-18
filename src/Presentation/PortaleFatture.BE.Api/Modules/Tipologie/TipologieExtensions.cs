using PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Api.Modules.Tipologie;

public static class TipologieExtensions
{
    public static EnteResponse Map(this Ente model)
    {
        return new EnteResponse
        {
            Descrizione = model.Descrizione,
            IdEnte = model.IdEnte
        };
    }
} 