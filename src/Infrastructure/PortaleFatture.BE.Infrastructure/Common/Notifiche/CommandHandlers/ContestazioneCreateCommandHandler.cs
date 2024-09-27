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

public class ContestazioneCreateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneCreateCommandHandler> logger) : IRequestHandler<ContestazioneCreateCommand, Contestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ContestazioneCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<Contestazione?> Handle(ContestazioneCreateCommand command, CancellationToken ct)
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

        Notifica? notifica;
        using var read = await _factory.Create(cancellationToken: ct);
        {
            notifica = await read.Query(new NotificaQueryGetByIdPersistence(new NotificaQueryGetById(command.AuthenticationInfo!, command.IdNotifica)), ct);
        }


        if (notifica == null 
            || notifica.StatoContestazione != (short)StatoContestazione.NonContestata
            || notifica.Fatturata != null && notifica.Fatturata == true 
            || notifica.TipologiaFattura != null)
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);

        command.Anno = Convert.ToInt32(notifica.Anno);
        command.Mese = Convert.ToInt32(notifica.Mese);

        var azione = await _handler.Send(new AzioneContestazioneQueryGetByIdNotifica(command.AuthenticationInfo, notifica.IdNotifica));

        if (azione == null || azione.CreazionePermessa == false)
            throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{notifica.Anno}-{notifica.Mese}"]);

        var adesso = azione.Calendario!.Adesso;
        command.DataInserimentoEnte = adesso;
        command.StatoContestazione = (short)StatoContestazione.ContestataEnte;
        command.Anno = Convert.ToInt32(notifica.Anno);
        command.Mese = Convert.ToInt32(notifica.Mese);

        using var uow = await _factory.Create(true, cancellationToken: ct);
        var id = await uow.Execute(new ContestazioneCreateCommandPersistence(command, _localizer), ct);
        if (id > 0)
        {
            var cont = new Contestazione()
            {
                DataInserimentoEnte = command.DataInserimentoEnte,
                Id = id,
                IdNotifica = command.IdNotifica,
                NoteEnte = command.NoteEnte,
                StatoContestazione = command.StatoContestazione,
                TipoContestazione = command.TipoContestazione,
                Anno = command.Anno,
                Mese = command.Mese
            };
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

        throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
    }
}