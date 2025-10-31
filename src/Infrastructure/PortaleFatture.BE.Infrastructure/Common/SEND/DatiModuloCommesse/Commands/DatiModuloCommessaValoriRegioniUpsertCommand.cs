using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

public class DatiModuloCommessaValoriRegioniUpsertCommand(IAuthenticationInfo authenticationInfo) : IRequest<bool?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public List<ValoriRegioneDto>? ValoriRegioni { get; set; }
} 