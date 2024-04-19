namespace PortaleFatture.BE.EmailSender.Models;

public class Configurazione
{
    public string? ConnectionString { get; set; }
    public string? From { get; set; }
    public string? Smtp { get; set; }
    public int SmtpPort { get; set; }
    public string? SmtpAuth { get; set; }
    public string? SmtpPassword { get; set; }
}