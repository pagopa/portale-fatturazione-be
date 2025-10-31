using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class DatiRegioniModuloCommessaQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ModuloCommessaRegioneDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}