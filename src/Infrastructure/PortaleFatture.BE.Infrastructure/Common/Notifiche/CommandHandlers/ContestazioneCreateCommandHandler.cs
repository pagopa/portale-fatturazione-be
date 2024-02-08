using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Core.Extensions;

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
        if (command.AuthenticationInfo!.Profilo != Profilo.PubblicaAmministrazione)
            throw new SecurityException(); //401 

        Notifica? notifica;
        using var read = await _factory.Create(cancellationToken: ct);
        {
            notifica = await read.Query(new NotificaQueryGetByIdPersistence(new NotificaQueryGetById(command.AuthenticationInfo!, command.IdNotifica)), ct);
        }

        if (notifica == null || notifica.StatoContestazione != (short)StatoContestazione.NonContestata)
            throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);

        command.Anno = Convert.ToInt32(notifica.Anno);
        command.Mese = Convert.ToInt32(notifica.Mese);

        var calendario = await _handler.Send(new CalendarioContestazioneQueryGet(command.AuthenticationInfo, command.Anno, command.Mese));

        if (!calendario.Valid)
            throw new ValidationException(_localizer["NotificaContestazioneValidationError", $"{calendario.AnnoContestazione}-{calendario.MeseContestazione}"]);
 

        command.DataInserimentoEnte = calendario.Adesso;
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
                    calendario.Adesso,
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
        }else
            uow.Rollback();

        throw new DomainException(_localizer["CreazioneContestazioneError", command.IdNotifica!]);
    }
}