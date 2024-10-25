using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class DatiModuloCommessaParzialiQueryGetByAnno : IRequest<IEnumerable<DatiModuloCommessaParzialiTotale>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }
    public DatiModuloCommessaParzialiQueryGetByAnno(IAuthenticationInfo authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }
    public int? AnnoValidita { get; set; }
}