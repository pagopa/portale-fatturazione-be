using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
public sealed class ReportNotificheByIdHashQueryCommand(IAuthenticationInfo authenticationInfo) : IRequest<ReportNotificheListDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? Hash { get { return Json.GetHashSHA256(); } }
    public string? Json { get; set; }
    public string? IdEnte { get; set; } = authenticationInfo.IdEnte; 
    public int Stato { get; set; } = 0;
}