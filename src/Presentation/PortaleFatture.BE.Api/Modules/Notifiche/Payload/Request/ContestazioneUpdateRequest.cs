namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class ContestazioneUpdateRequest
{
    public string? IdNotifica { get; set; }
    public string? NoteEnte { get; set; }
    public string? RispostaEnte { get; set; }
    public short StatoContestazione { get; set; }
}