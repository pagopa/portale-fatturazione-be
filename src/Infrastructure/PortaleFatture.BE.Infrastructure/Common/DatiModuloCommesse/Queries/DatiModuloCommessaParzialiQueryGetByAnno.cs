using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;

public class DatiModuloCommessaParzialiQueryGetByAnno : IRequest<IEnumerable<DatiModuloCommessaParzialiTotale>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }
    public DatiModuloCommessaParzialiQueryGetByAnno(IAuthenticationInfo authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    }
    public int? AnnoValidita { get; set; }
}