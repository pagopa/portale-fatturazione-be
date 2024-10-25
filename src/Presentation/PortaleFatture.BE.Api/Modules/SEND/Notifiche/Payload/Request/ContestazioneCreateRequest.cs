namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;

public class ContestazioneCreateRequest
{
    public int TipoContestazione { get; set; }
    public string? IdNotifica { get; set; }
    public string? NoteEnte { get; set; }
}