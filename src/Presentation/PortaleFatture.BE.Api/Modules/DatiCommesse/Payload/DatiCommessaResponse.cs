using System.Runtime.Serialization;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;

public record DatiCommessaResponse
{
    public long? Id { get; set; }
    public string? Cup { get; set; }
    public string? Cig { get; set; }
    public string? CodCommessa { get; set; }
    public DateTimeOffset DataDocumento { get; set; }
    public bool? SplitPayment { get; set; }
    public long? IdTipoContratto { get; set; }
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? FlagOrdineContratto { get; set; }
    public DateTimeOffset? DataCreazione { get; set; }
    public DateTimeOffset DataModifica { get; set; }
    public string? IdEnte { get; set; }
    public IEnumerable<DatiCommessaContattoResponse>? Contatti { get; set; }
}
