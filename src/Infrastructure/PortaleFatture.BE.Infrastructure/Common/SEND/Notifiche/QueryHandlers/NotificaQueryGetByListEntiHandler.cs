﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

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
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new NotificaQueryGetByListEntiPersistence(request), ct);
    }
}