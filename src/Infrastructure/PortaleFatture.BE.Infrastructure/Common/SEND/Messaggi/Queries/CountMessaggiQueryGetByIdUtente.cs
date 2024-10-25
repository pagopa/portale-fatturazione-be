using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;

public class CountMessaggiQueryGetByIdUtente(IAuthenticationInfo authenticationInfo) : IRequest<int?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}