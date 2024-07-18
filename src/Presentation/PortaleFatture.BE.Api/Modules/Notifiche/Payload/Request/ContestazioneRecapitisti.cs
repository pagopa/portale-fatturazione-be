using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class ContestazioneRecapitisti
{
    public string? IdNotifica { get; set; } 
    public string? Risposta  { get; set; }
    public short StatoContestazione { get; set; } = (short)PortaleFatture.BE.Core.Entities.Notifiche.StatoContestazione.RispostaRecapitista;
}