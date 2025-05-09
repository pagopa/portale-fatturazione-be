namespace PortaleFatture.BE.Function.API.Models;

public class Configurazione : IConfigurazione
{
    public string? ConnectionString { get; set; }
    public string? AESKey { get; set; }
    public string? CustomDomain { get; set; }
}