using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

public class DatiModuloCommessaParzialiTotale
{
    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }

    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; } 

    [Column("FkIdStato")]
    public string? Stato { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
    public decimal Totale { get; set; } 
    public decimal Digitale { get; set; } 
    public decimal AnalogicoARNazionali{ get; set; } 
    public decimal AnalogicoARInternazionali { get; set; } 
    public decimal Analogico890Nazionali { get; set; } 
    public bool Modifica { get; set; }
} 