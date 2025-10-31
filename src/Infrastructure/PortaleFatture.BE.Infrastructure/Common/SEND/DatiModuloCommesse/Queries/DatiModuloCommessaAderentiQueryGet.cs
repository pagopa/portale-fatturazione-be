using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries; 
 
public class DatiModuloCommessaAderentiQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<DatiModuloCommessaAderentiDto>
{
    public IAuthenticationInfo? AuthenticationInfo { get; private set; } = authenticationInfo;
    public string? IdEnte { get; set; }
}