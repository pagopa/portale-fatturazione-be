using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

[Table("PercentualeAnticipo")]
public class DatiConfigurazioneModuloCategoriaCommessa
{
    [Column("FKIdTipoContratto")]
    public long IdTipoContratto { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }

    [Column("FkIdCategoriaSpedizione")]
    public int IdCategoriaSpedizione { get; set; }
    public int Percentuale { get; set; } 
    public string? Descrizione { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime? DataModifica { get; set; }
    public DateTime DataInizioValidita { get; set; }
    public DateTime DataFineValidita { get; set; } 
} 