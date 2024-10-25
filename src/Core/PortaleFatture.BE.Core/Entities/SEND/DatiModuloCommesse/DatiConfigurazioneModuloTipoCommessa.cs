using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

[Table("CostoNotifiche")]
public class DatiConfigurazioneModuloTipoCommessa
{
    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [Column("FkIdTipoSpedizione")]
    public int IdTipoSpedizione { get; set; }
    public decimal MediaNotificaNazionale { get; set; }
    public decimal MediaNotificaInternazionale { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime? DataModifica { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public DateTime DataFineValidita { get; set; }
    public string? Descrizione { get; set; }
}