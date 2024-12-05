using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;

public class EnteQueryCodiceSDIGetById : IRequest<EnteContrattoDto?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public EnteQueryCodiceSDIGetById(IAuthenticationInfo? authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }
}