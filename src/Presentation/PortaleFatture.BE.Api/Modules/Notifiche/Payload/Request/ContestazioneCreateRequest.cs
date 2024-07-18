namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class ContestazioneCreateRequest
{
    public int TipoContestazione { get; set; }
    public string? IdNotifica { get; set; }
    public string? NoteEnte { get; set; }
}