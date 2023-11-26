using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

 
public class ModuloCommessaByAnnoDto
{
    public bool Modifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; } 
    public string? IdEnte { get; set; } 
    public long IdTipoContratto { get; set; }  
    public string? Stato { get; set; } 
    public string? Prodotto { get; set; } 
    public decimal Totale { get; set; }
    public Dictionary<int, ModuloCommessaMeseTotaleDto>? Totali { get; set; } 
} 

public class ModuloCommessaMeseTotaleDto
{
    public decimal TotaleCategoria { get; set; } 
    public int IdCategoriaSpedizione { get; set; } 
    public string? Tipo { get; set; }
}