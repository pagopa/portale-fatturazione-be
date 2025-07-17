using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public sealed class ReportNotificheQueryCountHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheQueryCountHandler> logger) : IRequestHandler<ReportNotificheQueryCount, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheQueryCountHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<int?> Handle(ReportNotificheQueryCount request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportNotificheQueryCountPersistence(request), ct);
    }
}