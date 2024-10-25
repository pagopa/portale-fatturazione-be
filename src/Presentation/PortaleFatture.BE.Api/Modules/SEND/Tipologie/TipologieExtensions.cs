using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;

namespace PortaleFatture.BE.Api.Modules.SEND.Tipologie;

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