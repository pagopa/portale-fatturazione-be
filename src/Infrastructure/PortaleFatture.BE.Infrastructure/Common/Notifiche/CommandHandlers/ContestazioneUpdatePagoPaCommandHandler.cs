using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class ContestazioneUpdatePagoPaCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneUpdatePagoPaCommandHandler> logger) : IRequestHandler<ContestazioneUpdatePagoPACommand, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<ContestazioneUpdatePagoPaCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<Contestazione?> Handle(ContestazioneUpdatePagoPACommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo!;

        if (!(authInfo.Profilo == Profilo.Approvigionamento
            || authInfo.Profilo == Profilo.Finanza
            || authInfo.Profilo == Profilo.Assistenza))
            throw new SecurityException(); //401 

        var azioneCommand = new AzioneContestazioneQueryGetByIdNotifica(authInfo, command.IdNotifica);
        var azione = await _handler.Send(azioneCommand, ct);
        var notifica = azione!.Notifica;
        var contestazione = azione!.Contestazione;

        if (contestazione == null 
            || notifica == null
            || notifica!.Fatturata != null 
            || notifica.TipologiaFattura != null)
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);

        if (azione == null
            || (azione.CreazionePermessa == false
            && azione.RispostaPermessa == false
            && azione.ChiusuraPermessa == false
            ))
            throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

        var adesso = DateTime.UtcNow.ItalianTime(); 

        if(contestazione.DataInserimentoSend == null)
            command.DataInserimentoSend = adesso;
        else
        {
            command.DataInserimentoSend = contestazione.DataInserimentoSend;
            command.DataModificaSend = adesso;
        }
     

        if (command.StatoContestazione == (short)StatoContestazione.Accettata)
        {
            if (!azione.ChiusuraPermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);
         
            command.ExpectedStatoContestazione = notifica.StatoContestazione; 
            command.DataChiusura = adesso; // vale stesso campo per tutti
            command.Onere = SoggettiContestazione.OnereContestazioneAccettazionePA(command.Onere!);
        }
        else if (command.StatoContestazione == (short)StatoContestazione.Chiusa)
        {
            if (!azione.ChiusuraPermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

            command.ExpectedStatoContestazione = notifica.StatoContestazione; 
            command.DataChiusura = adesso; // vale stesso campo per tutti
            command.Onere = SoggettiContestazione.OnereContestazioneChiusuraPA(command.Onere!);
        }
        else if (command.StatoContestazione == (short)StatoContestazione.RispostaSend)
        {
            if (!azione.RispostaPermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

            if (notifica.StatoContestazione == (short)StatoContestazione.RispostaSend
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaRecapitista
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaConsolidatore
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaEnte
                || notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
            {
                command.ExpectedStatoContestazione = notifica.StatoContestazione; 
            }
            else
                throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
        }
        else
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);


        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var id = await uow.Execute(new ContestazioneUpdatePagoPACommandPersistence(command, _localizer), ct);
            if (id > 0)
            {
                var contest = await uow.Query(new ContestazioneQueryGetByIdNotificaPersistence(new ContestazioneQueryGetByIdNotifica(command.AuthenticationInfo!, command.IdNotifica)), ct);
                command.AuthenticationInfo!.IdEnte = notifica.IdEnte;
                var rowAffected = await uow.Execute(new StoricoCreateCommandPersistence(new StoricoCreateCommand(
                     command.AuthenticationInfo!,
                     adesso,
                     TipoStorico.Contestazione,
                     contest.Serialize())), ct);

                if (rowAffected == 1)
                {
                    uow.Commit();
                    return contest;
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