using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;

public class MessaggioReadCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public bool Lettura { get; set; } = true;
    public long? IdMessaggio { get; set; }
    public string? IdUtente { get; set; } = authenticationInfo!.Id;
}