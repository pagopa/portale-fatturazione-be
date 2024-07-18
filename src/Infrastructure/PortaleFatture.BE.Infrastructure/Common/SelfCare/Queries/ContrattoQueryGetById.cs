using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;

public class ContrattoQueryGetById : IRequest<Contratto?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public ContrattoQueryGetById(IAuthenticationInfo? authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
}