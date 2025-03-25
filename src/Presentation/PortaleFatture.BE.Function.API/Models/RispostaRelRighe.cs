namespace PortaleFatture.BE.Function.API.Models;

public sealed class RispostaRelRighe
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Count { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; }
}