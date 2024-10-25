using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.QueryHandlers;

public class ReportQueryGetByIdHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportQueryGetByIdHandler> logger) : IRequestHandler<ReportQueryGetById, ReportDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ReportQueryGetByIdHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ReportDto?> Handle(ReportQueryGetById request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportQueryGetByIdPersistence(request), ct);
    }
}