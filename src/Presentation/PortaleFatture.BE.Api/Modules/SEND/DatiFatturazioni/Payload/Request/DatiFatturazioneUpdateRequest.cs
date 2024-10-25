using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneUpdateRequest
{
    [Required]
    public long Id { get; set; }

    public string? Cup { get; set; }
    [Required]
    public bool NotaLegale { get; set; }

    public string? CodCommessa { get; set; }

    public DateTime? DataDocumento { get; set; }
    [Required]
    public bool? SplitPayment { get; set; }

    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    [Required]
    public string? TipoCommessa { get; set; }
    [Required]
    public string? Pec { get; set; }
    public List<DatiFatturazioneContattoCreateRequest>? Contatti { get; set; }
}