using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public class FlagContestazioneQueryGetAll : IRequest<IEnumerable<FlagContestazione>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}