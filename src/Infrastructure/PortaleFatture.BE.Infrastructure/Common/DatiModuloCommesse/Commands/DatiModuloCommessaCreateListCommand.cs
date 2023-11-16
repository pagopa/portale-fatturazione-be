using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class DatiModuloCommessaCreateListCommand : IRequest<List<DatiModuloCommessa>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; set; }

    public List<DatiModuloCommessaCreateCommand>? DatiModuloCommessaListCommand { get; set; }
}