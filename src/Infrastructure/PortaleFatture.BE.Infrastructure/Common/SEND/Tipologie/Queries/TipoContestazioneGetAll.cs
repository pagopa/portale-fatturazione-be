using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;

public sealed class TipoContestazioneGetAll : IRequest<IEnumerable<TipoContestazione>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}