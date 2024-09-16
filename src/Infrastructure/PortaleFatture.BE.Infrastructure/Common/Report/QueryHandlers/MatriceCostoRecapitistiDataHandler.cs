using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries;
using PortaleFatture.BE.Infrastructure.Common.Report.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Report.QueryHandlers;

public class MatriceCostoRecapitistiDataHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<MatriceCostoRecapitistiDataHandler> logger) : IRequestHandler<MatriceCostoRecapitistiData, IEnumerable<MatriceCostoRecapitistiDataDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<MatriceCostoRecapitistiDataHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<MatriceCostoRecapitistiDataDto>?> Handle(MatriceCostoRecapitistiData request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new MatriceCostoRecapitistiDataPersistence(request), ct);
    }
}