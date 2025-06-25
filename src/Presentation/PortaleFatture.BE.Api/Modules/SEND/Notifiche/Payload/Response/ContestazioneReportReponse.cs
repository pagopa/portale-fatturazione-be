using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;

public class ContestazioneReportReponse
{
    public List<ReportContestazioneStepsWithLinkDto>? Steps { get; set; }
    public string? Validità { get; set; }
}