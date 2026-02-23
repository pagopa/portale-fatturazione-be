namespace PortaleFatture.BE.Function.API.Models;

public class Configurazione : IConfigurazione
{
    public string? ConnectionString { get; set; }
    public string? AESKey { get; set; }
    public string? CustomDomain { get; set; } 
    public StorageNotifiche? StorageNotifiche { get; set; }
}

public class StorageNotifiche
{
    public string? AccountName { get; set; }
    public string? AccountKey { get; set; }
    public string? BlobContainerName { get; set; }
} 