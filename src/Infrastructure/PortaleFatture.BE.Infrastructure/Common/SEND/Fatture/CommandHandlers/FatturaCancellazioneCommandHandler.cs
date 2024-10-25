using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FatturaCancellazioneCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaCancellazioneCommandHandler> logger) : IRequestHandler<FatturaCancellazioneCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaCancellazioneCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(FatturaCancellazioneCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var idTestate = await uow.Execute(new FatturaCancellazioneTestataCommandPersistence(command, _localizer), ct);
            if (idTestate == command.IdFatture!.Length)
            {
                var idRighe = await uow.Execute(new FatturaCancellazioneRigheCommandPersistence(command, _localizer), ct);

                if (idRighe > 0)  // eliminare fatture e righe
                {
                    var totaleEliminato = await uow.Execute(new FatturaEliminazioneCommandPersistence(command, _localizer), ct);
                    if (totaleEliminato == idTestate + idRighe)
                    {
                        //log request
                        await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                             command.AuthenticationInfo!,
                             DateTime.UtcNow.ItalianTime(),
                             command.Cancellazione ? TipoStorico.CancellazioneFatture : TipoStorico.RipristionFatture,
                             command.Serialize())), ct);

                        uow.Commit();
                        return true;
                    }
                    else
                        uow.Rollback();
                }
                else
                    uow.Rollback();
            }
            else
                uow.Rollback();
        }
        throw new DomainException(_localizer["xxx", command.IdFatture.Serialize()]);
    }
}