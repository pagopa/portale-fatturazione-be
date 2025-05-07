namespace PortaleFatture.BE.Function.API.Autenticazione.Payload;

public sealed class AuthenticationResponse()  
{ 
    public string? IdEnte { get; set; } 
    public DateTime Timestamp { get; set; } 
    public string? FunctionName { get; set; }
    public string? Stage { get; set; }
    public string? Method { get; set; }  
    public string? Uri { get; set; }
    public string? IpAddress { get; set; }
    public string? Id { get; set; }
} 