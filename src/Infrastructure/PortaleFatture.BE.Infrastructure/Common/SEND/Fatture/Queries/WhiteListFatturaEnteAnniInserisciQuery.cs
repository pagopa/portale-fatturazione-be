using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class WhiteListFatturaEnteAnniInserisciQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<WhiteListFatturaEnteAnniInserimentoDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; set; } = DateTime.UtcNow.Year;
    public string? TipologiaFattura { get; set; }
    public string? IdEnte { get; set; }
} 
 