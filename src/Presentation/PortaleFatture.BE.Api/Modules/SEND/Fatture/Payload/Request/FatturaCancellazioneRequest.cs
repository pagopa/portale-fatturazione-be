namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class FatturaCancellazioneRequest
{
    public long[]? IdFatture { get; set; }
    public bool Cancellazione { get; set; }
}