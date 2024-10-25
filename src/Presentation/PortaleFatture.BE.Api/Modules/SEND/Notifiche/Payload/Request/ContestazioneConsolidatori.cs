namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class ContestazioneConsolidatori
{
    public string? IdNotifica { get; set; }
    public string? Risposta { get; set; }
    public short StatoContestazione { get; set; } = (short)Core.Entities.SEND.Notifiche.StatoContestazione.RispostaConsolidatore;
}