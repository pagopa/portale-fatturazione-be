namespace PortaleFatture_BE_SendEmailFunction.Models;

public static class ConfigurazioneSENDRichiestaNotifiche
{
    public static string? ConnectionString { get; set; } 
    public static string? AccountName { get; set; }
    public static string? AccountKey { get; set; }
    public static string? BlobContainerName { get; set; }
}
 