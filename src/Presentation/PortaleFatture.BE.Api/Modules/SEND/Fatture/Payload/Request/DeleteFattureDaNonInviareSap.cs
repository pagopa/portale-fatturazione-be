using System.ComponentModel.DataAnnotations;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
public class DeleteFattureDaNonInviareSap
{
    [Required]
    public FatturaKey []  Fatture { get; set; } =  [ ];
}
