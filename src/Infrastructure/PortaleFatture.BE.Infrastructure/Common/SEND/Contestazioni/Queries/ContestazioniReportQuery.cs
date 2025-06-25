using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries; 

public class ContestazioniReportQuery(IAuthenticationInfo authenticationInfo) : IRequest<ReportContestazioniList?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int? Page { get; set; }
    public int? Size { get; set; } 
    public string? Anno { get; set; }
    public string? Mese { get; set; }  
    public string[]? IdEnti { get; set; } 
    public int[]? IdTipologiaReports { get; set; }
}