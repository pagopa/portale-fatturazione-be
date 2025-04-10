﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public class NotificaQueryGetByListEntiHandlerv2(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<NotificaQueryGetByListEntiHandler> logger) : IRequestHandler<NotificaQueryGetByListaEntiv2, NotificaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByListEntiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<NotificaDto?> Handle(NotificaQueryGetByListaEntiv2 request, CancellationToken ct)
    {
        var init = DateTime.UtcNow;
        using var rs = await _factory.Create(cancellationToken: ct);
        var result = await rs.Query(new NotificaQueryGetByListEntiPersistencev2(request), ct);

        var fine = DateTime.UtcNow;
        var secs = (fine - init).TotalSeconds;
        _logger.LogWarning($"Notifiche Admin Anno:{request.AnnoValidita} - Mese:{request.MeseValidita}" + ": {secs}", secs);
        return result;
    }
}