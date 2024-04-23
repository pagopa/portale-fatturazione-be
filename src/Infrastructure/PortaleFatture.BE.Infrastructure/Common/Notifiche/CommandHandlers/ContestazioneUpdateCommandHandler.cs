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
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class ContestazioneUpdateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler, 
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneUpdateCommandHandler> logger) : IRequestHandler<ContestazioneUpdateCommand, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<ContestazioneUpdateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 

    public async Task<Contestazione?> Handle(ContestazioneUpdateCommand command, CancellationToken ct)
    {
        if (command.AuthenticationInfo!.Profilo != Profilo.PubblicaAmministrazione &&
           command.AuthenticationInfo!.Profilo != Profilo.GestorePubblicoServizio &&
           command.AuthenticationInfo!.Profilo != Profilo.SocietaControlloPubblico &&
           command.AuthenticationInfo!.Profilo != Profilo.PrestatoreServiziPagamento &&
           command.AuthenticationInfo!.Profilo != Profilo.AssicurazioniIVASS &&
           command.AuthenticationInfo!.Profilo != Profilo.StazioneAppaltanteANAC &&
           command.AuthenticationInfo!.Profilo != Profilo.PartnerTecnologico 
           )
            throw new SecurityException(); //401  
 
    var azioneCommand = new AzioneContestazioneQueryGetByIdNotifica(command.AuthenticationInfo, command.IdNotifica);
        var azione = await _handler.Send(azioneCommand, ct);
        var notifica = azione!.Notifica;
        var contestazione = azione!.Contestazione; 

        if (contestazione == null
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
        command.DataModificaEnte = adesso;

        if (command.StatoContestazione == (short)StatoContestazione.ContestataEnte) // posso solo modificare la nota 
        {
            if(!azione.CreazionePermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

            if (notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
                command.ExpectedStatoContestazione = (short)StatoContestazione.ContestataEnte;
            command.RispostaEnte = null;
        }
        else if (command.StatoContestazione == (short)StatoContestazione.Annullata)
        {
            if (!azione.CreazionePermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

            if (notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
                command.ExpectedStatoContestazione = (short)StatoContestazione.ContestataEnte;
            else
                throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
            command.RispostaEnte = null;
            command.NoteEnte = contestazione.NoteEnte; 
        }
        else if (command.StatoContestazione == (short)StatoContestazione.Chiusa)
        {
            if (!azione.ChiusuraPermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

            command.ExpectedStatoContestazione = notifica.StatoContestazione;
            command.NoteEnte = contestazione.NoteEnte;
            command.DataChiusura = adesso; // vale stesso campo per tutti
            command.Onere = SoggettiContestazione.OnereContestazioneChiusuraEnte(command.Onere!, command.AuthenticationInfo.Profilo);
        }
        else if (command.StatoContestazione == (short)StatoContestazione.RispostaEnte)
        {
            if (!azione.RispostaPermessa)
                throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

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
                    uow.Rollback();  
            }
            else
                uow.Rollback();
        }
        throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
    }
}