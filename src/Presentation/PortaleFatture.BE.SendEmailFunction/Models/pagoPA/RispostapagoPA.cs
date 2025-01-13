namespace PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

public sealed class RispostapagoPA
{
    public int Anno { get; set; }
    public string? Trimestre { get; set; }
    public string? Tipologia  { get; set; }
    public string? Data { get; set; }
    public bool? DbConnection { get; set; } = true;
    public string? Error { get; set; } 
    public int NumeroInvio { get; set; }
}