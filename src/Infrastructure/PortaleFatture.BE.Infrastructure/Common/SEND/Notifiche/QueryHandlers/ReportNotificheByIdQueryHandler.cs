using CsvHelper;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using MediatR;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public sealed class ReportNotificheByIdQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheByIdQueryHandler> logger) : IRequestHandler<ReportNotificheByIdQueryCommand, ReportNotificheListDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheByIdQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportNotificheListDto?> Handle(ReportNotificheByIdQueryCommand request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportNotificheByIdQueryPersistence(request), ct);
    }
}