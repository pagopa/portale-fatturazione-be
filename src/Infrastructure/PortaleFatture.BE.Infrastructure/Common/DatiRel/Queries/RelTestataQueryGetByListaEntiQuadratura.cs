using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelTestataQueryGetByListaEntiQuadratura(IAuthenticationInfo authenticationInfo) : IRequest<RelTestataQuadraturaDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? EntiIds { get; set; }
    public string? IdContratto { get; set; } 
    public string? TipologiaFattura { get; set; } 
    public int? Anno { get; set; } 
    public int? Mese { get; set; } 
    public byte? Caricata { get; set; }
    public int? Page { get; set; }
    public int? Size { get; set; }
}