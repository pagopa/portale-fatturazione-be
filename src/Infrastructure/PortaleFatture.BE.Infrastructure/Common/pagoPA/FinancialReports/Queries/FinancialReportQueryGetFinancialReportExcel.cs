﻿using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
public class FinancialReportQueryGetFinancialReportExcel(IAuthenticationInfo authenticationInfo) : IRequest<IEnumerable<GridFinancialReportDto>>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public string[]? ContractIds { get; set; }
    public string? MembershipId { get; set; }
    public string? RecipientId { get; set; }
    public string? ABI { get; set; }
    public string[]? Quarters { get; set; }
    public string? Year { get; set; }
}