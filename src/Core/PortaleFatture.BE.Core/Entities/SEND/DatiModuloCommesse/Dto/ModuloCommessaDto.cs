using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

public class ModuloCommessaDto
{
    public int Anno { get; set; }
    public int Mese { get; set; }
    public bool Modifica { get; set; }
    public DateTime DataModifica { get; set; }
    public IEnumerable<DatiModuloCommessa>? DatiModuloCommessa { get; set; }
    public IEnumerable<DatiModuloCommessaTotale>? DatiModuloCommessaTotale { get; set; }
}