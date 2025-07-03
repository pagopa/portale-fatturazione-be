namespace PortaleFatture_BE_SendEmailFunction.Models;

public class EmailRelDataRequest
{
    public string? Anno { get; set; }
    public string? Mese { get; set; }
    public string? TipologiaFattura { get; set; } 
    public string? Ricalcola { get; set; } 
    public string? Data { get; set; }  
}