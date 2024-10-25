using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.QueryHandlers;

public class MatriceCostoRecapitistiHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<MatriceCostoRecapitistiHandler> logger) : IRequestHandler<MatriceCostoRecapitisti, IEnumerable<MatriceCostoRecapitistiDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<MatriceCostoRecapitistiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<MatriceCostoRecapitistiDto>?> Handle(MatriceCostoRecapitisti request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new MatriceCostoRecapitistiPersistence(request), ct);
    }
}