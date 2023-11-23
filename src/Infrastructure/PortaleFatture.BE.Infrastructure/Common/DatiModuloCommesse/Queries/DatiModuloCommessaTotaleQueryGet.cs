using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaTotaleQueryGet : IRequest<IEnumerable<DatiModuloCommessaTotale>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }
    public DatiModuloCommessaTotaleQueryGet(IAuthenticationInfo authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    } 
    public int AnnoValidita { get; set; } 
    public int MeseValidita { get; set; }
} 