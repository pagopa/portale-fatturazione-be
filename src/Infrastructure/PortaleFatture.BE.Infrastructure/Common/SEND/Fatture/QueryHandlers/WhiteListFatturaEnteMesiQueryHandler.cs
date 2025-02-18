using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class WhiteListFatturaEnteMesiQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<WhiteListFatturaEnteMesiQueryHandler> logger) : IRequestHandler<WhiteListFatturaEnteMesiQuery, IEnumerable<int>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<WhiteListFatturaEnteMesiQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<int>?> Handle(WhiteListFatturaEnteMesiQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new WhiteListFatturaEnteMesiQueryPersistence(request), ct);
    }
}