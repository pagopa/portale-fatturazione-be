namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class RelFatturabileByIdEntiRequest
{
    public int Anno { get; set; }
    public int Mese { get; set; } 
    public string[]? EntiIds { get; set; }
    public bool Fatturabile { get; set; }
}