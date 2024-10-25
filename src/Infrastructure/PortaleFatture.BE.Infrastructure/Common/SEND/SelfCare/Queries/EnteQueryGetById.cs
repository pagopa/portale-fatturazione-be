using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;

public class EnteQueryGetById : IRequest<Ente?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public EnteQueryGetById(IAuthenticationInfo? authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }
}