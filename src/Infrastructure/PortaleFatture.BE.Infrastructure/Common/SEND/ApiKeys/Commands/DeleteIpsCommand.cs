using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;

public sealed class DeleteIpsCommand(IAuthenticationInfo? authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;  
    public string? FkIdEnte { get; set; } = authenticationInfo!.IdEnte; 
    public string? IpAddress { get; set; }  
} 