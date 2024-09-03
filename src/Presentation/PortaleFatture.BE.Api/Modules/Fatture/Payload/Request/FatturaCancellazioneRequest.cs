namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class FatturaCancellazioneRequest
{
    public long[]? IdFatture { get; set; } 
    public bool Cancellazione { get; set; }
}