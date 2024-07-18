using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;


public class RelDocumentoDto
{ 
    public string? IdEnte { get; set; } 
 
    public string? RagioneSociale { get; set; } 
    public DateTime? DataDocumento { get; set; } 
    public string? IdDocumento { get; set; } 
    public string? Cup { get; set; } 
    public string? IdContratto { get; set; } 
    public string? TipologiaFattura { get; set; } 
    public string? Anno { get; set; } 
    public string? Mese { get; set; } 
    public string? TotaleAnalogico { get; set; } 
    public string? TotaleDigitale { get; set; } 
    public int TotaleNotificheAnalogiche { get; set; } 
    public int TotaleNotificheDigitali { get; set; } 
    public string? Totale { get; set; }
} 