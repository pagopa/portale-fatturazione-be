using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class RelBulkDownloadCommand(IAuthenticationInfo? authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; } = authenticationInfo;
    
    public List<RelDownloadCommand>? Commands { get; set; }
}