using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries; 
 
public class ContestazioniEnteAnniMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ContestazioneEnteAnniMesi>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}