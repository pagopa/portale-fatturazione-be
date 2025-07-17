using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ReportNotificheUpdateByIdCommand(IAuthenticationInfo? authenticationInfo) : IRequest<string?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? UniqueId { get; set; } 
    public string? InternalOrganizationId { get; } = authenticationInfo!.IdEnte;   
    public int IdReport { get; set; } 
    public string? LinkDocumento { get; set; }
}
