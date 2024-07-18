namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

public class RelQueryDto
{
    public string? IdEnte { get; set; } = null;
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; } 
    public string? TipologiaFattura { get; set; }
    public string? IdContratto { get; set; }  
    public byte? Caricata { get; set; }
    public string? Azione { get; set; }
    public string[]? EntiIds { get; set; } = null;
}