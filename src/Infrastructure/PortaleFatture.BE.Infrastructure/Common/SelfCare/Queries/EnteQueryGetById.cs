using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;

public class EnteQueryGetById : IRequest<Ente?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public EnteQueryGetById(IAuthenticationInfo? authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
} 