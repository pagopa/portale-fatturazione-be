using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
public class DatiModuloCommessaTotaleQueryGet(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<DatiModuloCommessaTotale>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? IdEnte { get; internal set; } = authenticationInfo.IdEnte;
    public int AnnoValidita { get; set; }
    public int MeseValidita { get; set; }
}