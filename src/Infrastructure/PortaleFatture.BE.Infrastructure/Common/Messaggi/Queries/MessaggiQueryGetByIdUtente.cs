using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries;

public class MessaggiQueryGetByIdUtente(IAuthenticationInfo authenticationInfo) : IRequest<MessaggioListaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;


    public int? Page { get; set; }
    public int? Size { get; set; } 
    public int? AnnoValidita { get; set; }
    public int? MeseValidita { get; set; } 
    public string[]? TipologiaDocumento { get; set; } 
    public bool? Letto { get; set; }
}