namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

 
public sealed class FatturePeriodoDto
{ 
    public int Mese { get; set; } 
    public int Anno { get; set; }
    public string? TipologiaFattura { get; set; }
    public DateTime? DataFattura { get; set; }
}