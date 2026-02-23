using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Function.API.Models;

namespace PortaleFatture.BE.Function.API.Notifiche.Payload;

public class NotificheRicercaInternalRequest 
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
    public string? IdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public string? IdContratto { get; set; }
    public int? IdReport { get; set; } 
    public Session? Session { get; set; }  
} 