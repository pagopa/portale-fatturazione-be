using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;

public class FattureQueryRicercaByEnte(IAuthenticationInfo authenticationInfo) : IRequest<FattureListaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string[]? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}