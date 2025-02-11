using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;

public class FatturaModificaTipoContrattoCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdEnte { get; set; }
    public int TipoContratto { get; set; } 
    public string? IdContratto { get; set; } 
    public int TipoContrattoPrecedente { get; set; }
    public DateTime DataInserimento { get; set; } = DateTime.UtcNow.ItalianTime();
    public DateTime DataCancellazione => DataInserimento;
    public string? IdUtente { get; set; } = authenticationInfo!.Id;
}