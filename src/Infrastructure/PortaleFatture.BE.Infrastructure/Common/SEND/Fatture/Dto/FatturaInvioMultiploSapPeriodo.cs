namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FatturaInvioMultiploSapPeriodo
{
    public long IdFattura { get; set; }
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; } 
    public string? RagioneSociale { get; set; } 
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; } 
    public decimal Importo { get; set; } 
    public DateTime DataFattura { get; set; }
} 