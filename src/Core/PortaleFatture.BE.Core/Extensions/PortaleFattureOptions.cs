namespace PortaleFatture.BE.Core.Extensions;

public class PortaleFattureOptions
{
    public string? ConnectionString { get; set; } 
    public string? SelfCareCertEndpoint { get; set; }
    public string? SelfCareUri { get; set; }
    public string? FattureSchema { get; set; }  
    public string? Vault { get; set; }
}  