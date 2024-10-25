using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;

public class ReportQueryGetById(IAuthenticationInfo authenticationInfo) : IRequest<ReportDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public long? IdReport { get; set; }
}