using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneCreateRequest
{

    public string? Cup { get; set; }
    [Required]
    public bool NotaLegale { get; set; }

    public string? CodCommessa { get; set; }

    public DateTime? DataDocumento { get; set; }

    [Required]
    public bool? SplitPayment { get; set; }

    public string? IdDocumento { get; set; }
    public string? Map { get; set; }
    public string? TipoCommessa { get; set; }
    public string? Pec { get; set; }
    public List<DatiFatturazioneContattoCreateRequest>? Contatti { get; set; }
}