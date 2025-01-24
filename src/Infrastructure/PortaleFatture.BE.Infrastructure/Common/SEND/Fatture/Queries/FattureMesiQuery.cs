using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? Anno { get; set; }
}