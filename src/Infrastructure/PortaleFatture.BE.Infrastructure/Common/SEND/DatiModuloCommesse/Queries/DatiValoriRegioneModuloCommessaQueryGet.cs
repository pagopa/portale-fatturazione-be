using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class DatiValoriRegioneModuloCommessaQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ValoriRegioneDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Aanno { get; set; }
    public int? Mese { get; set; }
    public string? IdEnte => AuthenticationInfo?.IdEnte;
}
