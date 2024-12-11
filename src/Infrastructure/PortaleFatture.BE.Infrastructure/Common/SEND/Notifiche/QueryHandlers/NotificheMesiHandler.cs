using global::PortaleFatture.BE.Core.Resources;
using global::PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;
public class NotificheMesiHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<NotificheMesiHandler> logger) : IRequestHandler<NotificheMesiQuery, IEnumerable<string>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<NotificheMesiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(NotificheMesiQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new NotificheMesiQueryPersistence(request), ct);
    }
}