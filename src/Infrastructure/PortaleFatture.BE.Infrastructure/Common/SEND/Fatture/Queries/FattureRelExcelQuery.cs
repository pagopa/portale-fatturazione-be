using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

public class FattureRelExcelQuery(IAuthenticationInfo authenticationInfo) : IRequest<List<IEnumerable<FattureRelExcelDto>>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; } 
    public int? FkIdTipoContratto { get; set; } 
    public int? FatturaInviata { get; set; }
}