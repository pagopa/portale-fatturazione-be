using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;


public class FatturaWhiteListCancellazioneCommand(IAuthenticationInfo? authenticationInfo, int[]? ids) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int[]? Ids { get; set; } = ids;
    public DateTime DataFine = DateTime.UtcNow.ItalianTime();
}