using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

public sealed class CreateIpsCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;  
    public string? FkIdEnte { get; set; } = authenticationInfo!.IdEnte;
    public DateTime? DataCreazione { get; set; } = DateTime.UtcNow.ItalianTime(); 
    public string? IpAddress { get; set; }  
} 