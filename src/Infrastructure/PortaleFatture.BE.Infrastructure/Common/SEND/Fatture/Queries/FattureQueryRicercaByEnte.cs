using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureQueryRicercaByEnte(IAuthenticationInfo authenticationInfo) : IRequest<FattureListaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}