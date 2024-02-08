namespace PortaleFatture.BE.Core.Entities.Notifiche.Dto;

public sealed class AzioneNotificaDto
{ 
    public bool ChiusuraPermessa { get; set; } 
    public bool ModificaPermessa { get; set; } 
    public Notifica? Notifica { get; set; } 
    public Contestazione? Contestazione { get; set; }
} 