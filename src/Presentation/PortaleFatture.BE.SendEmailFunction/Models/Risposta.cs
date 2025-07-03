namespace PortaleFatture_BE_SendEmailFunction.Models;

public sealed class Risposta
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? Data { get; set; }
    public int? Ricalcola { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; } 
    public string? Semestre { get; set; } 
    public string? Log { get; set; }
}

public sealed class RispostaRelRighe
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Count { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; }
}