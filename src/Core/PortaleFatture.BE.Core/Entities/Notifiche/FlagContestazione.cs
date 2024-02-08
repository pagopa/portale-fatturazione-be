using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.Notifiche;

public class FlagContestazione
{
    [Column("IdFlagContestazione")]
    public short Id { get; set; }

    [Column("FlagContestazione")]
    public string? Flag { get; set; }
} 