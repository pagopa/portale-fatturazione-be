using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Entities.Tipologie;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

public sealed class TipoContestazioneGetAll : IRequest<IEnumerable<TipoContestazione>>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
}