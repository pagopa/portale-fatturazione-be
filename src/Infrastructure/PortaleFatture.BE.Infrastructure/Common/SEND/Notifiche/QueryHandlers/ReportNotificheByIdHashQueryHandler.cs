using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public sealed class ReportNotificheByIdHashQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheByIdHashQueryHandler> logger) : IRequestHandler<ReportNotificheByIdHashQueryCommand, ReportNotificheListDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheByIdHashQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportNotificheListDto?> Handle(ReportNotificheByIdHashQueryCommand request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportNotificheByIdHashQueryCommandPersistence(request), ct);
    }
}