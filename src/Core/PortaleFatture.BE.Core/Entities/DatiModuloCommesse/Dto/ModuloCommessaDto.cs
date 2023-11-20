namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

public class ModuloCommessaDto
{
    public IEnumerable<DatiModuloCommessa>? DatiModuloCommessa { get; set; } 
    public IEnumerable<DatiModuloCommessaTotale>? DatiModuloCommessaTotale { get; set; }
} 