using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;

public class NotificaQueryGetByListaEnti(IAuthenticationInfo authenticationInfo) : IRequest<NotificaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoValidita { get; set; }
    public int? MeseValidita { get; set; }
    public string? Prodotto { get; set; }
    public string? Cap { get; set; }
    public string? Profilo { get; set; } 
    public TipoNotifica? TipoNotifica { get; set; } 
    public string? Iun { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; } 
    public StatoContestazione? StatoContestazione { get; set; } 
    public string[]? EntiIds { get; set; }
}