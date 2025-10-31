using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
 
public class DatiPrevisionaleCalendarioQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<DatiPrevisionaleCalendarioDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? AnnoRiferimento { get; set; }
    public int? MeseRiferimento { get; set; } 
} 