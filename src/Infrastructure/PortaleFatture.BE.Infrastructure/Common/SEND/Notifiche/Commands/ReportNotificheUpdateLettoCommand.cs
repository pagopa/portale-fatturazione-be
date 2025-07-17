using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ReportNotificheUpdateLettoCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? IdReport { get; set; } 
    public string? InternalOrganizationId { get; } = authenticationInfo!.IdEnte;
    public int Letto { get; set; } = 1;
    public int StatoAtteso { get; set; } = 0;

    public DateTime DataLettura { get; set; } = DateTime.UtcNow.ItalianTime();
}
