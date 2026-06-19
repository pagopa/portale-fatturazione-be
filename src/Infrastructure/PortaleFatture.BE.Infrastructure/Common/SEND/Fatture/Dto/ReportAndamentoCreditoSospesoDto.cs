namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

public sealed class ReportAndamentoCreditoSospesoDto
{
    public string? IdEnte { get; set; }
    public string? RagioneSociale { get; set; }
    public string? IdContratto { get; set; }
    public string? TipoContratto { get; set; }
    public string? TipologiaFattura { get; set; }
    public int NumFatturaSospesa { get; set; }
    public string? TipoDocumento { get; set; }
    public DateTime DataFattura { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public decimal ImponibileFattura { get; set; }
    public decimal CreditoCumulato { get; set; }
    public string? RelNonFirmata { get; set; }
}