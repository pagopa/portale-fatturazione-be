using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;

 
public class ApiKeyQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ApiKeyDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdEnte { get; set; } = authenticationInfo.IdEnte;
}