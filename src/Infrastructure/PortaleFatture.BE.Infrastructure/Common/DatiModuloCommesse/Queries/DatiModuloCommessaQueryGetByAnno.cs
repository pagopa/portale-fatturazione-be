using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGetByAnno : IRequest<IEnumerable<ModuloCommessaByAnnoDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; }
    public DatiModuloCommessaQueryGetByAnno(IAuthenticationInfo authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    } 
    public int? AnnoValidita { get; set; }  
} 