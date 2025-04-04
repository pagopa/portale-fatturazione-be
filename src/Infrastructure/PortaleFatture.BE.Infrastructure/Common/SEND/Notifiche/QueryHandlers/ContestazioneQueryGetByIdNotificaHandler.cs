﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.QueryHandlers;

public class ContestazioneQueryGetByIdNotificaHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneQueryGetByIdNotificaHandler> logger) : IRequestHandler<ContestazioneQueryGetByIdNotifica, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ContestazioneQueryGetByIdNotificaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<Contestazione?> Handle(ContestazioneQueryGetByIdNotifica request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ContestazioneQueryGetByIdNotificaPersistence(request), ct);
    }
}