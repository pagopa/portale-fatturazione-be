using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class RicercaContrattiTipologiaQuery(IAuthenticationInfo authenticationInfo) : IRequest<ContrattiTipologiaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; set; }
    public int? TipologiaContratto { get; set; } 
    public int? Page { get; set; }
    public int? Size { get; set; }
}