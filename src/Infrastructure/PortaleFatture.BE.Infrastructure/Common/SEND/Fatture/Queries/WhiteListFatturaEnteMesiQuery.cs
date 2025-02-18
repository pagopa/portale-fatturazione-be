using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class WhiteListFatturaEnteMesiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<int>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
 
    public int? Anno { get; set; } 
} 