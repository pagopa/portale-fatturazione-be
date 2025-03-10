using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FatturaRiptristinoSAPCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaRiptristinoSAPCommandHandler> logger) : IRequestHandler<FatturaRiptristinoSAPCommandList, bool>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaRiptristinoSAPCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool> Handle(FatturaRiptristinoSAPCommandList commands, CancellationToken ct)
    {
        var execute = true;
        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            try
            {
                foreach (var command in commands.Commands)
                {
                    var queryCommand = command.Map();
                    var result = await uow.Query(new FattureIdsQueryByParametersPersistence(queryCommand), ct);
                    if (result)
                    {
                        await uow.Execute(new FatturaRiptristinoSAPCommandPersistence(command, _localizer), ct);
 
                        await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                             command.AuthenticationInfo!,
                             DateTime.UtcNow.ItalianTime(),
                             command.Invio ? TipoStorico.InvioSAP : TipoStorico.AnnullaSAP,
                             queryCommand.Serialize())), ct);
                        execute &= true;
                    }
                    else
                        execute &= false;
                }
            }
            catch (Exception)
            { 
                execute &= false;
            }

            if(execute)
                uow.Commit();
            else
                uow.Rollback();
        }
        return execute;
    }
}