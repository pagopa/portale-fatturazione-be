namespace PortaleFatture.BE.Core.Auth;

public class AuthenticationInfo : IAuthenticationInfo
{
    public string? Id { get; set; }
    public long? IdTipoContratto { get; set; }
    public string? Prodotto { get; set; }
    public string? IdEnte { get; set; } 
    public string? NomeEnte { get; set; }
    public string? Profilo { get; set; }
    public string? Email { get; set; }
    public string? Ruolo { get; set; }
    public string? DescrizioneRuolo { get; set; }  
} 