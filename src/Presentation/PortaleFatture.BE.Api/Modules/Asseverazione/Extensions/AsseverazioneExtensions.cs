using PortaleFatture.BE.Api.Modules.Asseverazione.Payload.Response;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Commands;

namespace PortaleFatture.BE.Api.Modules.Asseverazione.Extensions;
public static class AsseverazioneExtensions
{
    public static AsseverazionExportResponse Mapper(this EnteAsserverazioneDto model)
    {

        return new AsseverazionExportResponse
        {
            DataAsseverazione = model.DataAsseverazione,
            Asseverazione = model.Asseverazione,
            IdEnte = model.IdEnte,
            RagioneSociale = model.RagioneSociale,
            Descrizione = model.Descrizione
        };
    }
}