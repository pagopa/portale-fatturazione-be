using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.EntiPrivati.Queries;

public class ReportPrivatiVBSQueryGetExcel(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<ReportPrivatiVBSDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo;
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ABI { get; set; }
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
}