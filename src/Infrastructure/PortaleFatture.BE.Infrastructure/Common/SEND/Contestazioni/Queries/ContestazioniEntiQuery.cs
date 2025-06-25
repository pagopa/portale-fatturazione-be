using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries; 

public class ContestazioniEntiQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ContestazioneEnte>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? Descrizione { get; set; }
}