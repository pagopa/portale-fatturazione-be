namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

public class ModuloCommessaDto
{
    public bool Modifica { get; set; } 
    public IEnumerable<DatiModuloCommessa>? DatiModuloCommessa { get; set; } 
    public IEnumerable<DatiModuloCommessaTotale>? DatiModuloCommessaTotale { get; set; }
} 