using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

public class DatiModuloCommessa
{
    public int NumeroNotificheNazionali { get; set; }
    public int NumeroNotificheInternazionali { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataModifica { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }

    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }

    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("FkIdTipoSpedizione")]
    public int IdTipoSpedizione { get; set; }

    public string? Stato { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }
}