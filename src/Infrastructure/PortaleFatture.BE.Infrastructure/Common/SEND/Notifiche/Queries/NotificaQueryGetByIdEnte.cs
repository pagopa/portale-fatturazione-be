using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

public class NotificaQueryGetByIdEnte(IAuthenticationInfo authenticationInfo) : IRequest<NotificaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoValidita { get; set; }
    public int? MeseValidita { get; set; }
    public string? Prodotto { get; set; }
    public string? Cap { get; set; }
    public string? Profilo { get; set; }
    public string? RecipientId { get; set; }
    public TipoNotifica? TipoNotifica { get; set; }
    public string? Iun { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
    public int[]? StatoContestazione { get; set; }
}