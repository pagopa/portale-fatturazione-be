using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries; 
public class RelAnniByIdEnteQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? IdContratto { get; set; }
}
