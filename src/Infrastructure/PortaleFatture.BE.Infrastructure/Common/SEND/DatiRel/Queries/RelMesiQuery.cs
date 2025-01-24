using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

public class RelMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? Anno { get; set; }
}