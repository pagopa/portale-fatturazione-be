using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Tipologie;

public sealed class TipoCommessa
{
    [Column("TipoCommessa")]
    public string? Id { get; set; }
    public string? Descrizione { get; set; }
}