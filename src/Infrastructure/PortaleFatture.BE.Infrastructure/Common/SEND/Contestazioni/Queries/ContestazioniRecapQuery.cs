using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;



public class ContestazioniRecapQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ContestazioneRecap>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;

    public string? Anno { get; set; }
    public string? Mese { get; set; } 
    public string? ContractId { get; set; } 
    public string? IdEnte { get; set; }
}