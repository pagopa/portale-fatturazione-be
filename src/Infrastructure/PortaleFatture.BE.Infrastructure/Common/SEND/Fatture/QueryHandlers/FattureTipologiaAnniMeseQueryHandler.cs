using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
 
public class FattureTipologiaAnniMeseQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureTipologiaAnniMeseQueryHandler> logger) : IRequestHandler<FattureTipologiaAnniMeseQuery, IEnumerable<string>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureTipologiaAnniMeseQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(FattureTipologiaAnniMeseQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureTipologiaAnniMeseQueryPersistence(request), ct);
    }
}