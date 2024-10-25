using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;

public class ReportQueryGetByRicerca(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ReportDto>?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public int? Anno { get; set; }
    public int? Mese { get; set; }
    public string? TipologiaDocumento { get; set; }
    public string? CategoriaDocumento { get; set; }
}