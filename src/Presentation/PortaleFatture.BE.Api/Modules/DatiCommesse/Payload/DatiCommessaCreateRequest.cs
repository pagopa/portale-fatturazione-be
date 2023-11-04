using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;

public record DatiCommessaCreateRequest
{
    [Required]
    public string? Cup { get; set; }
    [Required]
    public string? Cig { get; set; }
    [Required]
    public string? CodCommessa { get; set; }
    [Required]
    public DateTimeOffset DataDocumento { get; set; } 
    [Required]
    public bool? SplitPayment { get; set; } 
    public long? IdTipoContratto { get; set; }
    [Required]
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? FlagOrdineContratto { get; set; } 
    public List<DatiCommessaContattoCreateRequest>? Contatti { get; set; }
}
