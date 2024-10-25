namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;

public class TipologiaFattureRequest
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public bool? Cancellata { get; set; }
}