using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Commands;

public class FatturaCancellazioneCommand(IAuthenticationInfo? authenticationInfo, long[]? idFatture, bool cancellazione) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public long[]? IdFatture { get; set; } = idFatture;
    public bool Cancellazione { get; set; } = cancellazione;
}