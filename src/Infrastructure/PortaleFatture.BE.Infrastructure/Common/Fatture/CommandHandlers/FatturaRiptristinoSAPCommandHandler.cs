﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.CommandHandlers;

public class FatturaRiptristinoSAPCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaRiptristinoSAPCommandHandler> logger) : IRequestHandler<FatturaRiptristinoSAPCommand, bool>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaRiptristinoSAPCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool> Handle(FatturaRiptristinoSAPCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            try
            {
                var queryCommand = command.Map();
                var result = await uow.Query(new FattureIdsQueryByParametersPersistence(queryCommand), ct);
                if (result)
                {
                    await uow.Execute(new FatturaRiptristinoSAPCommandPersistence(command, _localizer), ct);

                    //log request
                    await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                         command.AuthenticationInfo!,
                         DateTime.UtcNow.ItalianTime(),
                         command.Invio ? TipoStorico.InvioSAP : TipoStorico.AnnullaSAP,
                         queryCommand.Serialize())), ct);

                    uow.Commit();
                    return true;
                }
                else
                {
                    uow.Rollback();
                    return false;
                } 
            }
            catch (Exception)
            {
                //log here
                uow.Rollback();
                return false;
            }
        }
    }
}