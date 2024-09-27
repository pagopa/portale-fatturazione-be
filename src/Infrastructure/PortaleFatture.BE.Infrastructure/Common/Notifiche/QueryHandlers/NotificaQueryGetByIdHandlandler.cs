using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.QueryHandlers;

public class NotificaQueryGetByIdHandlandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer, 
 ILogger<NotificaQueryGetByIdHandlandler> logger) : IRequestHandler<NotificaQueryGetById, Notifica?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByIdHandlandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    public async Task<Notifica?> Handle(NotificaQueryGetById request, CancellationToken ct)
    { 
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new NotificaQueryGetByIdPersistence(request), ct); 
    }
}