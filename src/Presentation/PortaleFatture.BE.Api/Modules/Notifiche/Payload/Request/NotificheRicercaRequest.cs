using PortaleFatture.BE.Core.Entities.Notifiche;
namespace PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;

public class NotificheRicercaRequest
{
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? Prodotto { get; set; }
    public string? Cap { get; set; }
    public string? Profilo { get; set; }
    public TipoNotifica? TipoNotifica { get; set; }
    public int[]? StatoContestazione { get; set; } 
    public string? Iun { get; set; } 
    public string? RecipientId { get; set; }
}