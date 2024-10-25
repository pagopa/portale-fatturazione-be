namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public class FatturaInvioSap
{
    public string? TipologiaFattura { get; set; }

    public long NumeroFatture { get; set; }
    public int AnnoRiferimento { get; set; }
    public int MeseRiferimento { get; set; }
    public int Azione { get; set; } // 0 resetta, 1 invio a sap, 2 disabilita 
    public int Ordine { get; set; }
}