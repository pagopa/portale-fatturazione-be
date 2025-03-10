namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FatturaInvioMultiploSap
{
    public string? TipologiaFattura { get; set; } 
    public long NumeroFatture { get; set; }
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; } 
    public decimal Importo { get; set; } 
    public int StatoInvio { get; set; }
}