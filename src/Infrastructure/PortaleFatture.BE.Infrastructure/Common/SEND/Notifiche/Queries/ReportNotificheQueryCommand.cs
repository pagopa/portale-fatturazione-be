using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public class ReportNotificheQueryCommand(IAuthenticationInfo authenticationInfo) : IRequest<ReportNotificheListCountDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public DateTime? Init { get; set; }
    public DateTime? End { get; set; } 
    public int? Page { get; set; }
    public int? Size { get; set; }
    public int? Ordinamento { get; set; } = 0; 
}