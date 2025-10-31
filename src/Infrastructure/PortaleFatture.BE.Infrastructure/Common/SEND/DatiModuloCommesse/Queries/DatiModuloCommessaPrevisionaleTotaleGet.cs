using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

public class DatiModuloCommessaPrevisionaleTotaleGet(IAuthenticationInfo? authenticationInfo) : IRequest<DatiConfigurazioneModuloCommessa>
{
    public IAuthenticationInfo? AuthenticationInfo { get; } = authenticationInfo;
    public string? Prodotto { get; set; }
    public string? IdEnte { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}