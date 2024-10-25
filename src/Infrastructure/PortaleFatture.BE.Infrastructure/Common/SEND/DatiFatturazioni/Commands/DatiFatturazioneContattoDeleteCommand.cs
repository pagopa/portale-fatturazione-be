using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;

public sealed class DatiFatturazioneContattoDeleteCommand : IRequest<DatiFatturazioneContatto>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long IdDatiFatturazione { get; set; }
}