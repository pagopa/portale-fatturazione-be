using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries;

public class BannerQuery(IAuthenticationInfo authenticationInfo) : IRequest<BannerDto>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}