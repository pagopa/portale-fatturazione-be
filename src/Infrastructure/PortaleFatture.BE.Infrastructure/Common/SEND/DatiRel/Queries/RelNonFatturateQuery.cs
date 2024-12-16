using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

public class RelNonFatturateQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<RelNonFatturataDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
}