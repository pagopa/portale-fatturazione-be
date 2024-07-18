using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByIdEnte : IRequest<DatiFatturazione?>
{
    public IAuthenticationInfo? AuthenticationInfo { get; internal set; }
    public DatiFatturazioneQueryGetByIdEnte(IAuthenticationInfo? authenticationInfo)
    {
        this.AuthenticationInfo = authenticationInfo;
    } 
} 