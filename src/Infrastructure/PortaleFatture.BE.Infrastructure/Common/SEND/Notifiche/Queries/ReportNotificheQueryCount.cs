using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public sealed class ReportNotificheQueryCount(IAuthenticationInfo authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? IdEnte { get; set; } = authenticationInfo.IdEnte;
}