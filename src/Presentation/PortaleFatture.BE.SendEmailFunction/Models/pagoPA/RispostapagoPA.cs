namespace PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

public sealed class RispostapagoPA
{
    public string? Environment { get; set; }
    public int Anno { get; set; }
    public string? Trimestre { get; set; }
    public string? Tipologia  { get; set; }
    public string? Data { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; } 
    public int NumeroInvio { get; set; }
    public int Processati { get; set; }
    public int Inviati { get; set; }
    public int Loggati { get; set; }
    public int LogAnteprime{ get; set; }

}