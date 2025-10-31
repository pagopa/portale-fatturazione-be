using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class ModuloCommessaPrevisionaleDownloadQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ModuloCommessaPrevisionaleDownloadDto>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; private set; } = authenticationInfo;
    public string[]? IdEnti { get; set; }
    public int? Anno { get; set; } 
    public int? Mese { get; set; }
}