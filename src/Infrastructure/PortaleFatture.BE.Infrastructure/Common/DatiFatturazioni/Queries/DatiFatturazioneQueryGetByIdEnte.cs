using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;

public class DatiFatturazioneQueryGetByIdEnte : IRequest<DatiFatturazione?>
{
    IAuthenticationInfo? AuthenticationInfo { get; set; }
    public string? IdEnte { get; set; }
} 