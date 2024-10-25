using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public class NotificaQueryGetById(IAuthenticationInfo authenticationInfo, string? idNotifica) : IRequest<Notifica?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdNotifica { get; internal set; } = idNotifica;
}