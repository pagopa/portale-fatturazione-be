using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Tipologie;

public class CategoriaSpedizione
{
    [Column("IdCategoriaSpedizione")]
    public int Id { get; set; }

    public string? Descrizione { get; set; }
    public string? Tipo { get; set; }
    public List<TipoSpedizione>? TipoSpedizione { get; set; }
}