using System.ComponentModel.DataAnnotations.Schema;

namespace PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;

public class DatiFatturazione
{
    [Column("IdDatiFatturazione")]
    public long Id { get; set; }
    public string? Cup { get; set; }
    public string? CodCommessa { get; set; }
    public DateTime? DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }

    [Column("FkIdEnte")]
    public string? IdEnte { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }

    [Column("FkTipoCommessa")]
    public string? TipoCommessa { get; set; }

    [Column("FkProdotto")]
    public string? Prodotto { get; set; }
    public string? Pec { get; set; }
    public bool NotaLegale { get; set; }
    public DateTimeOffset DataCreazione { get; set; }
    public DateTimeOffset? DataModifica { get; set; }
    public IEnumerable<DatiFatturazioneContatto>? Contatti { get; set; }
}