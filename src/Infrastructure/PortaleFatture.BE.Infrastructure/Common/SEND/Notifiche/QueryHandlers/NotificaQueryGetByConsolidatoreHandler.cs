﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public class NotificaQueryGetByConsolidatoreHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IMediator handler,
    ILogger<NotificaQueryGetByConsolidatoreHandler> logger) : IRequestHandler<NotificaQueryGetByConsolidatore, NotificaRECCONDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByConsolidatoreHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<NotificaRECCONDto?> Handle(NotificaQueryGetByConsolidatore request, CancellationToken ct)
    {
        var annoNotifica = request.AnnoValidita!.Value;
        var meseNotifica = request.MeseValidita!.Value;

        var calendario = await _handler.Send(new CalendarioContestazioneQueryGet(request.AuthenticationInfo, annoNotifica, meseNotifica));

        if (!calendario.ValidVisualizzazione)
            return new NotificaRECCONDto()
            {
                Count = 0,
                Notifiche = []
            };

        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new NotificaQueryGetByConsolidatorePersistence(request), ct);
    }
}