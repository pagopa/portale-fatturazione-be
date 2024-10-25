using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;

public class MatriceCostoRecapitistiData(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<MatriceCostoRecapitistiDataDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}