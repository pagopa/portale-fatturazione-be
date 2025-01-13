namespace PortaleFatture.BE.EmailPSPSender.Models;

public class Configurazione
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? ClientId { get; set; } 
    public string? ClientSecret { get; set; }
    public string? From { get; set; } 
    public string? FromName { get; set; }

    public string? To { get; set; }
    public string? ToName { get; set; }
}