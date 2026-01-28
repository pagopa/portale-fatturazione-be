using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FatturePeriodoEnteQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturePeriodoEnteQueryHandler> logger) : IRequestHandler<FatturePeriodoEnteQuery, IEnumerable<FatturePeriodoDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FatturePeriodoEnteQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<FatturePeriodoDto>> Handle(FatturePeriodoEnteQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct); 
        return await rs.Query(new FatturePeriodoEnteQueryPersistence(request), ct);
    }
}