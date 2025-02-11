using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FatturaModificaTipoContrattoCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaModificaTipoContrattoCommandHandler> logger) : IRequestHandler<FatturaModificaTipoContrattoCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaModificaTipoContrattoCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(FatturaModificaTipoContrattoCommand command, CancellationToken ct)
    {

        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(command.IdEnte), ct) ?? throw new DomainException($"Non ho trovato l'ente specificato {command.IdEnte!}");
            try
            { 
                command.IdContratto = contratto.IdContratto;
                command.TipoContrattoPrecedente = contratto.IdTipoContratto;
                await uow.Execute(new FatturaTipoContrattoUpdatePersistence(command, _localizer), ct);
                var rowAffected = await uow.Execute(new FatturaTipoContrattoInsertPersistence(command, _localizer), ct);
                if (rowAffected == 2)
                {
                    uow.Commit();
                    return true;
                }
                else
                {
                    uow.Rollback();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel salvare la tipologia contratto.");
                uow.Rollback();
                return false;
            }
        } 
    }
}