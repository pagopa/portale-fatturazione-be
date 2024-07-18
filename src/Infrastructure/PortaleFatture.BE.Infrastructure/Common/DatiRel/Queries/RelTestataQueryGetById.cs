using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelTestataQueryGetById(IAuthenticationInfo authenticationInfo) : IRequest<RelTestataDettaglioDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? IdTestata { get; set; }  
}