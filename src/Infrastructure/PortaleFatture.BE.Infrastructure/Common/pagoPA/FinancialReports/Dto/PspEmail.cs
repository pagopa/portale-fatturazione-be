namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

public class PspEmail
{
    public string? IdContratto { get; set; }
    public string? Tipologia  { get; set; }
    public int Anno { get; set; }
    public string? Trimestre { get; set; }
    public string? Email { get; set; }
    public string? RagioneSociale { get; set; } 
    public string? DetailReport { get; set; }
    public string? AgentReport { get; set; }
    public string? DiscountReport { get; set; }  
}

public sealed class PspEmailTracking : PspEmail
{
    public string? Messaggio { get; set; }
    public string? Data { get; set; }
    public byte Invio { get; set; }
}