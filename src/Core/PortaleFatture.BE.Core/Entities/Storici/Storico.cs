using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Storici;

public class Storico
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; } 
    public string? IdUtente { get; set; } 
    public DateTime DataEvento { get; set; } 
    public string? DescrizioneEvento { get; set; } 
    public string? JsonTransazione { get; set; }
} 