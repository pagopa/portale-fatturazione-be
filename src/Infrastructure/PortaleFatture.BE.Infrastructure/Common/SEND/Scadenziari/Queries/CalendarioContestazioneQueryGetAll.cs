using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

public class CalendarioContestazioneQueryGetAll(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<CalendarioContestazione>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
}