using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.Notifiche;

public class TipoContestazione
{
    [Column(name: "IdTipoContestazione")]
    public int Id { get; set; }

    [Column(name: "TipoContestazione")]
    public string? Tipo { get; set; }
}