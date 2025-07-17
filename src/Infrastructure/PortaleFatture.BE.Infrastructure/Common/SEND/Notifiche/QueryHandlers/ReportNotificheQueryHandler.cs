using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;
 
public sealed class ReportNotificheQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheQueryHandler> logger) : IRequestHandler<ReportNotificheQueryCommand, ReportNotificheListCountDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportNotificheListCountDto?> Handle(ReportNotificheQueryCommand request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportNotificheQueryPersistence(request), ct);
    }
}