using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.QueryHandlers;

public class NotificaQueryGetByListEntiHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer, 
 ILogger<NotificaQueryGetByListEntiHandler> logger) : IRequestHandler<NotificaQueryGetByListaEnti, NotificaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByListEntiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    public async Task<NotificaDto?> Handle(NotificaQueryGetByListaEnti request, CancellationToken ct)
    { 
        using var rs = await _factory.Create(true, cancellationToken: ct);
        return await rs.Query(new NotificaQueryGetByListEntiPersistence(request), ct); 
    }
}