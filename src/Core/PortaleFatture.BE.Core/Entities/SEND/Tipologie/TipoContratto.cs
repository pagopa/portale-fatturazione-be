using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.Tipologie;

[Table("TipoContratto")]
public sealed class TipoContratto
{
    [Column("IdTipoContratto")]
    public long Id { get; set; }
    public string? Descrizione { get; set; }
}