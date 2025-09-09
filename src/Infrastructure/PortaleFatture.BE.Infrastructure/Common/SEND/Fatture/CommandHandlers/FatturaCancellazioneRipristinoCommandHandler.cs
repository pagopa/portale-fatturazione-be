using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FatturaCancellazioneRipristinoCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaCancellazioneRipristinoCommandHandler> logger) : IRequestHandler<FatturaCancellazioneRipristinoCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaCancellazioneRipristinoCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(FatturaCancellazioneRipristinoCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var rowAffected = await uow.Execute(new FatturaCancellazioneRipritistinoCommandPersistence(command, _localizer), ct); 
        if (rowAffected == 1)
        {
            using var uow2 = await _factory.Create(cancellationToken: ct);
            await uow2.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                     command.AuthenticationInfo!,
                     DateTime.UtcNow.ItalianTime(),
                     command.Cancellazione ? TipoStorico.CancellazioneFatture : TipoStorico.RipristionFatture,
                     command.Serialize())), ct);
            return true;
        }
        else
            return false;
    }
}