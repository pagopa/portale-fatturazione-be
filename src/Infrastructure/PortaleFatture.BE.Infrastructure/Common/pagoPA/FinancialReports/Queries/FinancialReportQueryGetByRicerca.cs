using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
public class FinancialReportQueryGetByRicerca(IAuthenticationInfo authenticationInfo) : IRequest<GridFinancialReportListDto>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ABI { get; set; }
    public string[]? Quarters { get; set; }
}