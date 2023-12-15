using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneCreateRequest
{
    [Required]
    public string? Cup { get; set; }
    [Required]
    public bool  NotaLegale { get; set; }
    [Required]
    public string? CodCommessa { get; set; }
    [Required]
    public DateTime DataDocumento { get; set; }
    [Required]
    public bool? SplitPayment { get; set; }
    [Required]
    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? TipoCommessa { get; set; }
    public string? Pec { get; set; }
    public List<DatiFatturazioneContattoCreateRequest>? Contatti { get; set; }
}