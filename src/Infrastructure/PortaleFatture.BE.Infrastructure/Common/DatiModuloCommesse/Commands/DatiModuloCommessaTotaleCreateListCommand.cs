using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

public class DatiModuloCommessaTotaleCreateListCommand : IRequest<List<DatiModuloCommessaTotale>>
{
    public IAuthenticationInfo? AuthenticationInfo { get; set; }

    public List<DatiModuloCommessaTotaleCreateCommand>? DatiModuloCommessaTotaleListCommand { get; set; }
}