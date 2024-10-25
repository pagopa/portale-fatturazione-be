using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByIdEnte : IRequest<DatiFatturazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public DatiFatturazioneQueryGetByIdEnte(IAuthenticationInfo? authenticationInfo)
    {
        AuthenticationInfo = authenticationInfo;
    }
}