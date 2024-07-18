using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class DatiModuloCommessaCreateListCommand(IAuthenticationInfo authenticationInfo) : IRequest<ModuloCommessaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public List<DatiModuloCommessaCreateCommand>? DatiModuloCommessaListCommand { get; set; }
}