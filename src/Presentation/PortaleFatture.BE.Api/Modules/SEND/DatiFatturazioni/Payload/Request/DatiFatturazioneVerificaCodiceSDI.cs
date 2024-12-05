using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneVerificaCodiceSDI
{
    [Required]
    public string? IdEnte { get; set; }

    [Required]
    public string? CodiceSDI { get; set; }
}

public record DatiFatturazioneVerificaCodiceSDIEnte
{  
    [Required]
    public string? CodiceSDI { get; set; }
}