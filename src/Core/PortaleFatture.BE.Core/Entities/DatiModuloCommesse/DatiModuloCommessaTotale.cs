using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

[Table("DatiModuloCommessaTotali")]
public class DatiModuloCommessaTotale
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }

    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("FkIdCategoriaSpedizione")]
    public int IdCategoriaSpedizione { get; set; }

    [Column("FkIdStato")]
    public string? Stato { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; } 
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; } 
    public decimal TotaleCategoria { get; set; }
} 