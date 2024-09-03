using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Report.QueryHandlers;

public class ReportQueryGetByRicercaHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportQueryGetByRicercaHandler> logger) : IRequestHandler<ReportQueryGetByRicerca, IEnumerable<ReportDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ReportQueryGetByRicercaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<ReportDto>?> Handle(ReportQueryGetByRicerca request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportQueryGetByRicercaPersistence(request), ct);
    }
}