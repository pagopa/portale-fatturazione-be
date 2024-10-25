using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

public class EnteContrattoDto
{

    [Column("IdEnte")]
    public string? IdEnte { get; set; }

    [Column("RagioneSociale")]
    public string? RagioneSociale { get; set; }

    [Column("TipoContratto")]
    public string? TipoContratto { get; set; }

    [Column("IdContratto")]
    public string? IdContratto { get; set; }
}
