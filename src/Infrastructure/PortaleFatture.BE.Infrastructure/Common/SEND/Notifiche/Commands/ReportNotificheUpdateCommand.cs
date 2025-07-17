using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;

public class ReportNotificheUpdateCommand(IAuthenticationInfo? authenticationInfo) : IRequest<string?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? UniqueId { get; set; } 
    public string? InternalOrganizationId { get; } = authenticationInfo!.IdEnte; 
    public int Stato { get; set; } 
    public int StatoAtteso { get; set; } 
    public DateTime DataFine { get; } = DateTime.UtcNow.ItalianTime(); 
    public string? NomeDocumento { get; set; } 
    public long? Count { get; set; }
}
