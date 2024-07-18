namespace PortaleFatture_BE_SendEmailFunction.Models;

public static class Configurazione
{
    public static string? ConnectionString { get; set; }
    public static string? From { get; set; }
    public static string? Smtp { get; set; }
    public static int SmtpPort { get; set; }
    public static string? SmtpAuth { get; set; } 
    public static string? SmtpPassword { get; set; } 
}