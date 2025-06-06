﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.CommandHandlers;

public class RelDownloadCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<RelUploadCreateCommandHandler> logger) : IRequestHandler<RelDownloadCommand, bool?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<RelUploadCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(RelDownloadCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        {
            var rowAffected = await uow.Execute(new RelUploadCreateCommandPersistence(command.Map(), _localizer), ct);
            return rowAffected == 1;
        }
        throw new DomainException(_localizer["xxx"]);
    }
}