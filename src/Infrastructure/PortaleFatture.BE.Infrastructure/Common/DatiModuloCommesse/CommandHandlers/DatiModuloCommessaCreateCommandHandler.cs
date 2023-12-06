using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class DatiModuloCommessaCreateCommandHandler(
 IFattureDbContextFactory factory,
 IScadenziarioService scadenziarioService,
 IStringLocalizer<Localization> localizer,
 ILogger<DatiModuloCommessaCreateCommandHandler> logger) : IRequestHandler<DatiModuloCommessaCreateListCommand, ModuloCommessaDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiModuloCommessaCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IScadenziarioService _scadenziarioService = scadenziarioService;

    public async Task<ModuloCommessaDto?> Handle(DatiModuloCommessaCreateListCommand command, CancellationToken ct)
    {
        var (annoAttuale, meseAttuale, giornoAttuale, adesso) = Time.YearMonthDayFatturazione();

        var (valid, scadenziario) = await _scadenziarioService.GetScadenziario(command.AuthenticationInfo, TipoScadenziario.DatiModuloCommessa, annoAttuale, meseAttuale);

        if (!valid)
            throw new ValidationException(_localizer["DataScadenziarioValidationError", $"{scadenziario.GiornoInizio}-{scadenziario.GiornoFine}"]);

        var idTipoContratto = command.AuthenticationInfo.IdTipoContratto;
        var prodotto = command.AuthenticationInfo.Prodotto;
        var idEnte = command.AuthenticationInfo.IdEnte;
        var stato = string.Empty;

        Dictionary<int, decimal> categorieTotale = [];
        IEnumerable<CategoriaSpedizione>? categorie;
        DatiConfigurazioneModuloCommessa? confModuloCommessa = null;

        using (var rs = await _factory.Create(true, cancellationToken: ct))
        {
            var prodotti = await rs.Query(new ProdottoQueryGetAllPersistence(), ct);
            if (prodotti.IsNullNotAny())
            {
                var msg = "Provide products in configurazion!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }
            prodotto = prodotti.Where(x => x.Nome!.ToLower() == prodotto!.ToLower()).Select(x => x.Nome).FirstOrDefault();
            if (prodotto == null)
            {
                var msg = "I could not find the specified product!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }
            var contratti = await rs.Query(new TipoContrattoQueryGetAllPersistence(), ct);
            if (contratti.IsNullNotAny())
            {
                var msg = "Provide contracts in configurazion!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }
            idTipoContratto = contratti.Where(x => x.Id! == idTipoContratto!).Select(x => x.Id).FirstOrDefault();
            if (idTipoContratto == null)
            {
                var msg = "I could not find the specified coontract!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }

            categorie = await rs.Query(new SpedizioneQueryGetAllPersistence());
            confModuloCommessa = await rs.Query(new DatiConfigurazioneModuloCommessaQueryGetPersistence(idTipoContratto.Value, prodotto), ct);

            var statoCommessa = await rs.Query(new StatoCommessaQueryGetByDefaultPersistence(), ct);
            stato = statoCommessa!.Stato;
        }

        var commandTotale = command.GetTotali(categorie, confModuloCommessa, idEnte, annoAttuale, meseAttuale, idTipoContratto.Value, prodotto, stato);


        foreach (var cmd in command.DatiModuloCommessaListCommand!) // validazione per id tipo spedizione
        {
            cmd.Stato = stato;
            cmd.Prodotto = prodotto;
            cmd.IdTipoContratto = idTipoContratto.Value;
            cmd.AnnoValidita = annoAttuale;
            cmd.MeseValidita = meseAttuale;
            cmd.DataCreazione = adesso;
            cmd.DataModifica = adesso;

            var (error, errorDetails) = DatiModuloCommessaValidator.Validate(cmd);
            if (!string.IsNullOrEmpty(error))
                throw new DomainException(_localizer[error, errorDetails]);

            cmd.ValoreNazionali = commandTotale.ParzialiTipoCommessa![cmd.IdTipoSpedizione].ValoreNazionali;
            cmd.ValoreInternazionali = commandTotale.ParzialiTipoCommessa![cmd.IdTipoSpedizione].ValoreInternazionali;
            cmd.PrezzoNazionali = commandTotale.ParzialiTipoCommessa![cmd.IdTipoSpedizione].PrezzoNazionali;
            cmd.PrezzoInternazionali = commandTotale.ParzialiTipoCommessa![cmd.IdTipoSpedizione].PrezzoInternazionali;
        }

        using var uow = await _factory.Create(true, cancellationToken: ct);
        try
        {
            var rowAffected = await uow.Execute(new DatiModuloCommessaCreateCommandPersistence(command), ct);
            if (rowAffected == command.DatiModuloCommessaListCommand!.Count)
            {
                rowAffected = await uow.Execute(new DatiModuloCommessaCreateTotaleCommandPersistence(commandTotale), ct);
                if (rowAffected == commandTotale.DatiModuloCommessaTotaleListCommand!.Count)
                    uow.Commit();
                else
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["DatiModuloCommessaError", idEnte!]);
                }
            }
            else
            {
                uow.Rollback();
                throw new DomainException(_localizer["DatiModuloCommessaError", idEnte!]);
            }
        }
        catch (Exception e)
        {
            uow.Rollback();
            var methodName = nameof(DatiConfigurazioneModuloCommessaCreateCommandHandler);
            _logger.LogError(e, "Errore nel salvataggio del modulo commessa: \"{MethodName}\" per tipo ente: \"{idEnte}\"", methodName, idEnte);
            throw new DomainException(_localizer["DatiModuloCommessaError", idEnte!]);
        }
        var datic = await uow.Query(new DatiModuloCommessaQueryGetByIdPersistence(idEnte, annoAttuale, meseAttuale, idTipoContratto, prodotto), ct);
        var datit = await uow.Query(new DatiModuloCommessaTotaleQueryGetByIdPersistence(idEnte, annoAttuale, meseAttuale, idTipoContratto, prodotto), ct);
        var moduloCommessa = new ModuloCommessaDto()
        {
            Modifica = valid && stato == StatoModuloCommessa.ApertaCaricato,
            DatiModuloCommessa = datic!,
            DatiModuloCommessaTotale = datit!,
            Anno = datic!.Select(x=>x.AnnoValidita).FirstOrDefault(),
            Mese = datic!.Select(x => x.MeseValidita).FirstOrDefault(),
        };

        using var usto = await _factory.Create(cancellationToken: ct);
        await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
             command.AuthenticationInfo,
             adesso,
             TipoStorico.DatiModuloCommessa,
             moduloCommessa.Serialize())), ct);

        return moduloCommessa;
    }
}