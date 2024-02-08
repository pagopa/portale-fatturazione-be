using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Entities.Scadenziari.Dto;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class ContestazioneUpdateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IScadenziarioService scadenziarioService,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneUpdateCommandHandler> logger) : IRequestHandler<ContestazioneUpdateCommand, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<ContestazioneUpdateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IScadenziarioService _scadenziarioService = scadenziarioService;

    public async Task<Contestazione?> Handle(ContestazioneUpdateCommand command, CancellationToken ct)
    {
        if (command.AuthenticationInfo!.Profilo != Profilo.PubblicaAmministrazione)
            throw new SecurityException(); //401 

        var azioneCommand = new AzioneContestazioneQueryGetByIdNotifica(command.AuthenticationInfo, command.IdNotifica);
        var azione = await _handler.Send(azioneCommand, ct);
        var notifica = azione!.Notifica;
        var contestazione = azione!.Contestazione;

        if (notifica == null // non deve essere in uno stato iniziale o di "chiusura"
            || contestazione == null
            || notifica.Fatturata == true
            || notifica.StatoContestazione == (short)StatoContestazione.Annullata
            || notifica.StatoContestazione == (short)StatoContestazione.Accettata
            || notifica.StatoContestazione == (short)StatoContestazione.Chiusa
            || notifica.StatoContestazione == (short)StatoContestazione.NonContestata)
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);


        if (command.StatoContestazione != (short)StatoContestazione.Chiusa)
        {
            var calendario = await _handler.Send(new CalendarioContestazioneQueryGet(command.AuthenticationInfo, Convert.ToInt16(notifica.Anno), Convert.ToInt16(notifica.Mese)));

            if (!calendario.Valid)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{calendario.AnnoContestazione}-{calendario.MeseContestazione}"]);
        }

        var adesso = DateTime.UtcNow.ItalianTime();
        command.DataModificaEnte = adesso;


        if (command.StatoContestazione == (short)StatoContestazione.ContestataEnte) // posso solo modificare la nota 
        {
            if (notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
                command.ExpectedStatoContestazione = (short)StatoContestazione.ContestataEnte;
            else
                command.ExpectedStatoContestazione = (short)StatoContestazione.Annullata;
            command.RispostaEnte = null;
        }
        else if (command.StatoContestazione == (short)StatoContestazione.Annullata)
        {
            if (notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
                command.ExpectedStatoContestazione = (short)StatoContestazione.ContestataEnte;
            else
                command.ExpectedStatoContestazione = (short)StatoContestazione.Annullata;
            command.RispostaEnte = null;
            command.NoteEnte = contestazione.NoteEnte;
        }
        else if (command.StatoContestazione == (short)StatoContestazione.Chiusa)
        {
            command.ExpectedStatoContestazione = notifica.StatoContestazione;
            command.NoteEnte = contestazione.NoteEnte;
            command.DataChiusura = adesso; // vale stesso campo per tutti
            command.Onere = command.AuthenticationInfo.Profilo;
        }
        else if (command.StatoContestazione == (short)StatoContestazione.RispostaEnte)
        {
            if (notifica.StatoContestazione == (short)StatoContestazione.RispostaSend
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaRecapitista
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaConsolidatore
                || notifica.StatoContestazione == (short)StatoContestazione.RispostaEnte)
            {
                command.ExpectedStatoContestazione = notifica.StatoContestazione;
                command.NoteEnte = contestazione.NoteEnte;
            }
            else
                throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
        }
        else
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);


        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var id = await uow.Execute(new ContestazioneUpdateCommandPersistence(command, _localizer), ct);
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
                {
                    uow.Rollback();
                    throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
                }

            }
            else
                uow.Rollback();
        }
        throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
    }
}