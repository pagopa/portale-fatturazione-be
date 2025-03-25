namespace PortaleFatture.BE.Function.API.Models;

public static class ConfigurazioneSEND
{
    public static string? ConnectionString { get; set; } 
    public static string? StorageRELAccountName { get; set; }
    public static string? StorageRELAccountKey { get; set; }
    public static string? StorageRELBlobContainerName { get; set; }
}
