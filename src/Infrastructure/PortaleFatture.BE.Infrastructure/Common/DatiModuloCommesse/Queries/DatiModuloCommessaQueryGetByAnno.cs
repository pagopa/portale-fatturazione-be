using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
public class DatiModuloCommessaQueryGetByAnno(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ModuloCommessaByAnnoDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoValidita { get; set; }  
} 