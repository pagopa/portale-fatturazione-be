using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneUpdateRequest
{
    [Required]
    public long Id { get; set; }
    [Required]
    public string? Cup { get; set; }
    [Required]
    public string? Cig { get; set; }
    [Required]
    public string? CodCommessa { get; set; }
    [Required]
    public DateTime DataDocumento { get; set; }
    [Required]
    public bool? SplitPayment { get; set; }
    [Required]
    public string? IdDocumento { get; set; }
    public string? Map { get; set; } 
    [Required]
    public string? TipoCommessa { get; set; }
    [Required]
    public string? Pec { get; set; }
    public List<DatiFatturazioneContattoCreateRequest>? Contatti { get; set; }
}