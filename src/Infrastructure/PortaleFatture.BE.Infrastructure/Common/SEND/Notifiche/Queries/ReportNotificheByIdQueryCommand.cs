using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public sealed class ReportNotificheByIdQueryCommand(IAuthenticationInfo authenticationInfo) : IRequest<ReportNotificheListDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? IdReport { get; set; }
    public string? IdEnte { get; set; } = authenticationInfo.IdEnte;
}