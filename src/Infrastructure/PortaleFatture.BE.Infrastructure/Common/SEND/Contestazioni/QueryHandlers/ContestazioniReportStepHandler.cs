using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.QueryHandlers;

public class ContestazioniReportStepHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioniReportStepHandler> logger) : IRequestHandler<ContestazioniReportStepQuery, ReportContestazioneByIdDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ContestazioniReportStepHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportContestazioneByIdDto?> Handle(ContestazioniReportStepQuery request, CancellationToken ct)
    {
        var report = new ReportContestazioneByIdDto();
        using var rs = await _factory.Create(true, cancellationToken: ct);
        report.Steps = await rs.Query(new ContestazioniReportStepsQueryPersistence(request), ct);
        report.ReportContestazione = (await rs.Query(new ContestazioniReportByIdQueryPersistence(request), ct))!.FirstOrDefault(); 
        return report;
    }
}