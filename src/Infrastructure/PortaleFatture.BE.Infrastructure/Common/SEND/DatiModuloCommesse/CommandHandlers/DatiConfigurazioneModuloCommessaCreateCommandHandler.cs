using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.CommandHandlers;

public class DatiConfigurazioneModuloCommessaCreateCommandHandler : IRequestHandler<DatiConfigurazioneModuloCommessaCreateCommand, DatiConfigurazioneModuloCommessa?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiConfigurazioneModuloCommessaCreateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public DatiConfigurazioneModuloCommessaCreateCommandHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiConfigurazioneModuloCommessaCreateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiConfigurazioneModuloCommessa?> Handle(DatiConfigurazioneModuloCommessaCreateCommand command, CancellationToken ct)
    {
        var tipi = command.Tipi;

        foreach (var cmd in tipi!)
        {
            var (error, errorDetails) = DatiConfigurazioneModuloCommessaValidator.ValidateTipo(cmd);
            if (!string.IsNullOrEmpty(error))
                throw new DomainException(_localizer[error, errorDetails]);
        }

        var categorie = command.Categorie;
        foreach (var cmd in categorie!)
        {
            var (error, errorDetails) = DatiConfigurazioneModuloCommessaValidator.ValidateCategoria(cmd);
            if (!string.IsNullOrEmpty(error))
                throw new DomainException(_localizer[error, errorDetails]);
        }

        // verifico configurazione e valido
        using var dbContext = await _factory.Create(cancellationToken: ct);
        var spedizioneConfig = await dbContext.Query(new SpedizioneQueryGetAllPersistence());
        var (errorct, errorDetailsct) = spedizioneConfig!.ValidateCategorieConfiguazione(command);
        if (!string.IsNullOrEmpty(errorct))
            throw new DomainException(_localizer[errorct, errorDetailsct]);

        var count = command.Tipi!.Count() + command.Categorie!.Count();
        var firstTipoRequest = command.Tipi!.FirstOrDefault();
        var idTipoContratto = firstTipoRequest!.IdTipoContratto;
        var prodotto = firstTipoRequest.Prodotto;
        var dataCreazione = command.Tipi!.FirstOrDefault()!.DataCreazione!.Value;

        // verifico se esiste già un valore per la configurazione modulo commessa 
        using var uow = await _factory.Create(true, cancellationToken: ct);
        var lastValue = await uow.Query(new DatiConfigurazioneModuloCommessaQueryGetPersistence(idTipoContratto: idTipoContratto, prodotto: prodotto));

        try
        {
            if (lastValue != null)
            {
                var lastInizioValidità = lastValue.Tipi!.FirstOrDefault()!.DataInizioValidita;
                var lastIdTipoContratto = lastValue.Tipi!.FirstOrDefault()!.IdTipoContratto;
                var lastProdotto = lastValue.Tipi!.FirstOrDefault()!.Prodotto;
                var updateCommand = new DatiConfigurazioneModuloCommessaUpdateCommand()
                {
                    Tipo = new DatiConfigurazioneModuloCommessaUpdateTipoCommand()
                    {
                        IdTipoContratto = lastIdTipoContratto,
                        Prodotto = lastProdotto,
                        DataModifica = dataCreazione,
                        DataFineValidita = dataCreazione,
                        DataInizioValidita = lastInizioValidità
                    },
                    Categoria = new DatiConfigurazioneModuloCommessaUpdateCategoriaCommand()
                    {
                        IdTipoContratto = lastIdTipoContratto,
                        Prodotto = lastProdotto,
                        DataModifica = dataCreazione,
                        DataFineValidita = dataCreazione,
                        DataInizioValidita = lastInizioValidità
                    }
                };
                var updateRowAffected = await uow.Execute(new DatiConfigurazioneModuloCommessaUpdateCommandPersistence(updateCommand), ct);
                if (updateRowAffected != count)
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiConfigurazioneModuloCommessaError"]);
                }
            }

            var rowAffected = await uow.Execute(new DatiConfigurazioneModuloCommessaCreateCommandPersistence(command), ct);
            if (rowAffected != count)
            {
                uow.Rollback();
                throw new DomainException(_localizer["DatiConfigurazioneModuloCommessaError"]);
            }
            else
                uow.Commit();
            return await uow.Query(new DatiConfigurazioneModuloCommessaQueryGetPersistence(idTipoContratto: idTipoContratto, prodotto: prodotto));
        }
        catch (Exception e)
        {
            uow.Rollback();
            var methodName = nameof(DatiConfigurazioneModuloCommessaCreateCommandHandler);
            _logger.LogError(e, "Errore nel salvataggio dei dati configurazione: \"{MethodName}\" per tipo contratto: \"{TipoContratto}\"", methodName, firstTipoRequest.IdTipoContratto);
            throw new DomainException(_localizer["DatiConfigurazioneModuloCommessaError"]);
        }
    }
}