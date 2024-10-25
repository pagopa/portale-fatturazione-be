using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;

public sealed class DatiFatturazioneContattoCreateListCommand : IRequest<DatiFatturazioneContatto>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }

    List<DatiFatturazioneContattoCreateCommand>? DatiFatturazioneContattoLista { get; set; }
}