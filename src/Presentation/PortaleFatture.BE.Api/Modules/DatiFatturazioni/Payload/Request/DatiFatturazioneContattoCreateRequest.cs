using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;

public record DatiFatturazioneContattoCreateRequest
{
    [Required]
    public string? Email { get; set; } 
}