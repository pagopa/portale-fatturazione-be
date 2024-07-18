using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;

public class FattureRelExcelQuery(IAuthenticationInfo authenticationInfo) : IRequest<List<IEnumerable<FattureRelExcelDto>>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? IdEnti { get; set; }
    public string? TipologiaFattura { get; set; }
    public int? Anno { get; set; }
    public int? Mese { get; set; }
}