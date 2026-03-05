using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FattureRiepilogoAnniHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureRiepilogoAnniHandler> logger) : IRequestHandler<FattureRiepilogoAnniQuery, IEnumerable<string>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureRiepilogoAnniHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(FattureRiepilogoAnniQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureRiepilogoAnniQueryPersistence(request), ct);
    }
}
