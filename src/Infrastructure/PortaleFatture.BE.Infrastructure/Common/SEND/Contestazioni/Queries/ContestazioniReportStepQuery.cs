using MediatR;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries; 

public class ContestazioniReportStepQuery(IAuthenticationInfo authenticationInfo) : IRequest<ReportContestazioneByIdDto?>
{
    public IAuthenticationInfo AuthenticationInfo { get; internal set; } = authenticationInfo; 
    public int? IdReport { get; set; }
}