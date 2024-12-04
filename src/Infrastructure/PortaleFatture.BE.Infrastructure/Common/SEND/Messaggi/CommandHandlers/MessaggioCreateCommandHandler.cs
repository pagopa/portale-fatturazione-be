﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.CommandHandlers;

public class MessaggioCreateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<MessaggioCreateCommandHandler> logger) : IRequestHandler<MessaggioCreateCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<MessaggioCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<bool?> Handle(MessaggioCreateCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var id = await uow.Execute(new MessaggioCreateCommandPersistence(command, _localizer), ct);
        if (id > 0)
            return true;
        else
            return false;
    }
}