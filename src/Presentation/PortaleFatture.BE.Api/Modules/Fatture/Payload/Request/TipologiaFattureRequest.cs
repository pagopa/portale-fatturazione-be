namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class TipologiaFattureRequest
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public bool? Cancellata { get; set; }
}