using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.QueryHandlers;

public class ContestazioniReportHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioniReportHandler> logger) : IRequestHandler<ContestazioniReportQuery, ReportContestazioniList?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ContestazioniReportHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportContestazioniList?> Handle(ContestazioniReportQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ContestazioniReportQueryPersistence(request), ct);
    }
}