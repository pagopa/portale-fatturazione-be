using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;

public class MessaggioUpdateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public DateTime DataStepCorrente { get; set; } = DateTime.UtcNow.ItalianTime();
    public short Stato { get; set; } = 2; // 0 presa in carico - 1 elaborazione - 2 completata - 3 disabilitata 
    public string? LinkDocumento { get; set; }
    public string? IdUtente { get; set; } = authenticationInfo!.Id;
    public string? IdEnte { get; set; } = authenticationInfo!.IdEnte;
    public string? Hash { get; set; }
}