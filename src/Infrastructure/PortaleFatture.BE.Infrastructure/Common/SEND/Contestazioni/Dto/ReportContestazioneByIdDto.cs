namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

public class ReportContestazioneByIdDto
{
    public ReportContestazioni? ReportContestazione { get; set; } 
    public IEnumerable<ReportContestazioneStepsDto>? Steps { get; set; }
} 