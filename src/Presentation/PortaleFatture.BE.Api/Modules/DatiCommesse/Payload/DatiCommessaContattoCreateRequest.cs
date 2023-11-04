using System.ComponentModel.DataAnnotations;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;

public record DatiCommessaContattoCreateRequest
{
    [Required]
    public string?  Email { get; set; }
    [Required]
    public int Tipo { get; set; }
}
