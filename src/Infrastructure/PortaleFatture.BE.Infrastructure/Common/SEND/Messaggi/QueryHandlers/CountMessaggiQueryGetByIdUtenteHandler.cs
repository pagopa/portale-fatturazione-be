using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.QueryHandlers;

public class CountMessaggiQueryGetByIdUtenteHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<NotificaQueryGetByIdEnteHandler> logger) : IRequestHandler<CountMessaggiQueryGetByIdUtente, int?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByIdEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<int?> Handle(CountMessaggiQueryGetByIdUtente request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new CountMessaggiQueryGetByIdUtentePersistence(request), ct);
    }
}