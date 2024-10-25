using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;

public class ContrattoQueryGetById : IRequest<Contratto?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public ContrattoQueryGetById(IAuthenticationInfo? authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }
}