using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetById : IRequest<DatiFatturazione>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public long Id { get; set; }
} 