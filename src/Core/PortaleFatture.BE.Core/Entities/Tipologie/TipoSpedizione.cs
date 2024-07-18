using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Tipologie;

public class TipoSpedizione
{
    [Column("IdTipoSpedizione")]
    public int Id { get; set; }
    public string? Descrizione { get; set; }

    [Column("FkIdCategoriaSpedizione")]
    public int IdCategoriaSpedizione { get; set; }
    public string? Tipo { get; set; }
}