namespace PortaleFatture.BE.Function.API.Notifiche.Payload;

public sealed class NotificheResponse
{
    public string? LinkDocumento { get; set; }
    public int Stato { get; set; } 
    public string? UniqueId { get; set; }  
    public int Count { get; set; }
} 