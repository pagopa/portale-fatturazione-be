using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Tipologie;

[Table("Stato")]
public class StatoCommessa
{
    public string? Stato { get; set; } 
    public bool Default { get; set; }
} 