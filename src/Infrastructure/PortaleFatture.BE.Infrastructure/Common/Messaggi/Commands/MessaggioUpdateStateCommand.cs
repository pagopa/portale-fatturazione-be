using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class MessaggioUpdateStateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;    
    public short Stato { get; set; }  // 0 presa in carico - 1 elaborazione - 2 completata - 3 disabilitata  

    public string? IdUtente { get; set; } = authenticationInfo?.Id;
    public long? IdMessaggio { get; set; }
}