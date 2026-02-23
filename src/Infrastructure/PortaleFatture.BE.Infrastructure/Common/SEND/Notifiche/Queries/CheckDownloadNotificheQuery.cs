using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries; 
 
public class CheckDownloadNotificheQuery(IAuthenticationInfo authenticationInfo) : IRequest<CheckDownloadNotificheDto>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string IdEnte { get; internal set; } = authenticationInfo!.IdEnte!;
    public int Anno { get; set; } 
    public int Mese { get; set; } 
    public DateTime? Date { get; set; }
}