using MediatR;
using PortaleFatture.BE.Core.Auth;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
public class FinancialReportQuartersQuery(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<string>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string? Year { get; set; }
}