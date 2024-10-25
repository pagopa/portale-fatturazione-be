using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public class FlagContestazioneQueryGetAll : IRequest<IEnumerable<FlagContestazione>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}