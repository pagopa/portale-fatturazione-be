using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

 
public class FatturePeriodoEnteQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<FatturePeriodoDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdEnte => AuthenticationInfo.IdEnte;
}