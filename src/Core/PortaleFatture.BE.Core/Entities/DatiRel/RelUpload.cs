using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiRel;

public class RelUpload
{  
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }

    [Column("description")]
    public string? RagioneSociale { get; set; }

    [Column("contract_id")]
    public string? IdContratto { get; set; }

    [Column("TipologiaFattura")]
    public string? TipologiaFattura { get; set; }

    [Column("year")]
    public int? Anno { get; set; }

    [Column("month")]
    public int? Mese { get; set; }

    [Column("DataEvento")]
    public DateTime? DataEvento { get; set; }

    [Column("IdUtente")]
    public string? IdUtente { get; set; }
}  