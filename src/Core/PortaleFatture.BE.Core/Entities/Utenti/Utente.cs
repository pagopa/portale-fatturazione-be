using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Utenti;

[Table("Utenti")]
public class Utente
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; } 
    public string? IdUtente { get; set; } 
    public DateTime DataPrimo { get; set; } 
    public DateTime DataUltimo { get; set; }
} 