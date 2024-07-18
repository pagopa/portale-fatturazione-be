namespace PortaleFatture.BE.EmailSender.Models;

public sealed class Risposta
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? Data { get; set; }
    public int? Ricalcola { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; }
}