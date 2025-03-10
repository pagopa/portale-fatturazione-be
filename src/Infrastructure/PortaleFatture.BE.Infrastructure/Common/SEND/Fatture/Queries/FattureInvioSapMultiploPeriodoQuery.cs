using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureInvioSapMultiploPeriodoQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<FatturaInvioMultiploSapPeriodo>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int? AnnoRiferimento { get; set; }
    public int? MeseRiferimento { get; set; }
    public string? TipologiaFattura { get; set; }
}