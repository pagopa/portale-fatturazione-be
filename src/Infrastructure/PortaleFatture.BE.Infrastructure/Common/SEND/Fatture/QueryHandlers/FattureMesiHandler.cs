using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FattureMesiHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureMesiHandler> logger) : IRequestHandler<FattureMesiQuery, IEnumerable<string>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureMesiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(FattureMesiQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureMesiQueryPersistence(request), ct);
    }
}