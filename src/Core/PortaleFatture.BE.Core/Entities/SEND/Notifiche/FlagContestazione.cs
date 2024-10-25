using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.Notifiche;

public class FlagContestazione
{
    [Column("IdFlagContestazione")]
    public short Id { get; set; }

    [Column("FlagContestazione")]
    public string? Flag { get; set; }

    [Column("Descrizione")]
    public string? Descrizione { get; set; }
}