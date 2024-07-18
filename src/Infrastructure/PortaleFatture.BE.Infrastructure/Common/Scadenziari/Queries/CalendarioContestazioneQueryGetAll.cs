using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;

public class CalendarioContestazioneQueryGetAll(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<CalendarioContestazione>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;  
}