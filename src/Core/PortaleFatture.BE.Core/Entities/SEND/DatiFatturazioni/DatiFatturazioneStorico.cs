using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;

public class DatiFatturazioneStorico
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }

    [Column("JsonDatiFatturazione")]
    public string? DatiFatturazione { get; set; }
}