using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.Tipologie;

[Table("Prodotti")]
public class Prodotto
{
    [Column("Prodotto")]
    public string? Nome { get; set; }
}