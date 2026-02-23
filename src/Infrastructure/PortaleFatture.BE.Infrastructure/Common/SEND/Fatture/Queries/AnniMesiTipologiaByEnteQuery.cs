using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class AnniMesiTipologiaByEnteQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<AnniMesiTipologiaByEnteDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? IdEnte { get; set; }  = authenticationInfo.IdEnte;
}