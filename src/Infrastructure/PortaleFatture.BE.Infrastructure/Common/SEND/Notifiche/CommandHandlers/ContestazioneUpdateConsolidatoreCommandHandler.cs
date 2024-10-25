using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.CommandHandlers;

public class ContestazioneUpdateConsolidatoreCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneUpdateConsolidatoreCommandHandler> logger) : IRequestHandler<ContestazioneUpdateConsolidatoreCommand, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<ContestazioneUpdateConsolidatoreCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<Contestazione?> Handle(ContestazioneUpdateConsolidatoreCommand command, CancellationToken ct)
    {
        var azioneCommand = new AzioneContestazioneQueryGetByIdNotifica(command.AuthenticationInfo!, command.IdNotifica);
        var azione = await _handler.Send(azioneCommand, ct);
        var notifica = azione!.Notifica;
        var contestazione = azione!.Contestazione;

        if (contestazione == null
            || notifica!.Fatturata != null
            || notifica.TipologiaFattura != null)
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);

        if (azione == null
            || azione.CreazionePermessa == false
            && azione.RispostaPermessa == false
            && azione.ChiusuraPermessa == false
            )
            throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

        if (notifica.Consolidatore != command.AuthenticationInfo!.IdEnte)
            throw new DomainException(_localizer["NotificaContestazioneValidationError", command.IdNotifica!]);

        var adesso = DateTime.UtcNow.ItalianTime();
        if (contestazione.DataInserimentoConsolidatore == null)
            command.DataInserimentoConsolidatore = adesso;
        else
        {
            command.DataInserimentoConsolidatore = contestazione.DataInserimentoConsolidatore;
            command.DataModificaConsolidatore = adesso;
        }


        if (contestazione.StatoContestazione == (short)StatoContestazione.ContestataEnte
            || contestazione.StatoContestazione == (short)StatoContestazione.RispostaEnte
            || contestazione.StatoContestazione == (short)StatoContestazione.RispostaConsolidatore
            || contestazione.StatoContestazione == (short)StatoContestazione.RispostaRecapitista
            || contestazione.StatoContestazione == (short)StatoContestazione.RispostaSend) // posso rispondere
        {
            if (command.StatoContestazione == (short)StatoContestazione.RispostaConsolidatore && azione.RispostaPermessa)
            {
                command.ExpectedStatoContestazione = notifica.StatoContestazione;
            }
            else if (command.StatoContestazione == (short)StatoContestazione.Accettata && azione.RispostaPermessa)
            {
                command.Onere = Profilo.Consolidatore;
                command.NoteConsolidatore ??= contestazione.NoteConsolidatore;
                command.ExpectedStatoContestazione = notifica.StatoContestazione;
                command.DataChiusura = adesso;
            }
            else
                throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
        }
        else
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);


        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var id = await uow.Execute(new ContestazioneUpdateConsolidatoreCommandPersistence(command, _localizer), ct);
            if (id > 0)
            {
                var cont = await uow.Query(new ContestazioneQueryGetByIdNotificaPersistence(new ContestazioneQueryGetByIdNotifica(command.AuthenticationInfo, command.IdNotifica)), ct);
                var rowAffected = await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                     command.AuthenticationInfo,
                     adesso,
                     TipoStorico.Contestazione,
                     cont.Serialize())), ct);

                if (rowAffected == 1)
                {
                    uow.Commit();
                    return cont;
                }
                else
                    uow.Rollback();
            }
            else
                uow.Rollback();
        }
        throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
    }
}