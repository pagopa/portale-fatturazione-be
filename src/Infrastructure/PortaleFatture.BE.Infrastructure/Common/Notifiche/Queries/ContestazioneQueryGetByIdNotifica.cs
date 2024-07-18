using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;

public class ContestazioneQueryGetByIdNotifica(IAuthenticationInfo authenticationInfo, string? idNotifica) : IRequest<Contestazione?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? IdNotifica { get; internal set; } = idNotifica;
}