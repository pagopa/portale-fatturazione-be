using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class DatiModuloCommessaCreateCommandHandler : IRequestHandler<DatiModuloCommessaCreateListCommand, List<DatiModuloCommessa>?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiModuloCommessaCreateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public DatiModuloCommessaCreateCommandHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiModuloCommessaCreateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }
    public async Task<List<DatiModuloCommessa>?> Handle(DatiModuloCommessaCreateListCommand command, CancellationToken ct)
    {
        var adesso = DateTime.UtcNow.ItalianTime();
        var (anno, mese) = adesso.YearMonth();

        //validare calendario
        var prodotto = string.Empty;
        long idTipoContratto = 0;
        using (var rs = await _factory.Create(true, cancellationToken: ct))
        {
            var prodotti = await rs.Query(new ProdottoQueryGetAllPersistence(), ct);
            prodotto = prodotti.FirstOrDefault()!.Nome;

            var contratti = await rs.Query(new TipoContrattoQueryGetAllPersistence(), ct);
            idTipoContratto = contratti.Select(x => x.Id).FirstOrDefault();
        } 

        var idEnte = string.Empty;
        List<DatiModuloCommessa> moduli = new();
        foreach (var cmd in command.DatiModuloCommessaListCommand!)
        { 
            idEnte = cmd.IdEnte!;
            cmd.Stato ??= StatoModuloCommessa.ApertaCaricato;
            cmd.Prodotto = prodotto;
            cmd.IdTipoContratto = idTipoContratto;
            cmd.AnnoValidita = anno;
            cmd.MeseValidita = mese;
            cmd.DataCreazione = adesso;
            cmd.DataModifica = adesso;

            var (error, errorDetails) = DatiModuloCommessaValidator.Validate(cmd);
            if (!string.IsNullOrEmpty(error))
                throw new DomainException(_localizer[error, errorDetails]);

            moduli.Add(new DatiModuloCommessa()
            {
                AnnoValidita = cmd.AnnoValidita,
                MeseValidita = cmd.MeseValidita,
                IdEnte = cmd.IdEnte,
                IdTipoContratto = cmd.IdTipoContratto,
                IdTipoSpedizione = cmd.IdTipoSpedizione,
                NumeroNotificheNazionali = cmd.NumeroNotificheNazionali,
                NumeroNotificheInternazionali = cmd.NumeroNotificheInternazionali,
                Stato = cmd.Stato
            });
        }

        using var uow = await _factory.Create(true, cancellationToken: ct);
        try
        {
            var rowAffected = await uow.Execute(new DatiModuloCommessaCreateCommandPersistence(command));
            if (rowAffected == command.DatiModuloCommessaListCommand!.Count)
                uow.Commit();
            else
            {
                uow.Rollback();
                throw new DomainException(_localizer["xxx"]);
            }
        }
        catch (Exception e)
        {
            uow.Rollback();
            var methodName = nameof(DatiConfigurazioneModuloCommessaCreateCommandHandler);
            _logger.LogError(e, "Errore nel salvataggio del modulo commessa: \"{MethodName}\" per tipo ente: \"{idEnte}\"", methodName, idEnte);
            throw new DomainException(_localizer["xxx"]);
        }
        return moduli;
    }
}