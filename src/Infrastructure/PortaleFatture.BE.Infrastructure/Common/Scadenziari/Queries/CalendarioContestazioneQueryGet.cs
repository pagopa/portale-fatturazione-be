using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;

public class CalendarioContestazioneQueryGet(IAuthenticationInfo authenticationInfo, int anno, int mese) : IRequest<CalendarioContestazione>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int Anno { get; internal set; } = anno;
    public int Mese { get; internal set; } = mese;  
}