using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries;

public sealed class KPIPagamentiMatriceQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<KPIPagamentiMatriceDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string? YearQuarter { get; set; }
}