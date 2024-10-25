using PortaleFatture.BE.Api.Modules.SEND.Asseverazione.Payload.Response;
using PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.Asseverazione.Extensions;
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