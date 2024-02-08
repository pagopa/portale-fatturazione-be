using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Entities.Notifiche.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.QueryHandlers;

public class AzioneContestazioneQueryGetByIdNotificaHandler(
 ISelfCareDbContextFactory factory,
 IFattureDbContextFactory fattureFactory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<AzioneContestazioneQueryGetByIdNotificaHandler> logger) : IRequestHandler<AzioneContestazioneQueryGetByIdNotifica, AzioneNotificaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IFattureDbContextFactory _fattureFactory = fattureFactory;
    private readonly ILogger<AzioneContestazioneQueryGetByIdNotificaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<AzioneNotificaDto?> Handle(AzioneContestazioneQueryGetByIdNotifica request, CancellationToken ct)
    {
        using var nt = await _factory.Create(cancellationToken: ct);
        var notifica = await nt.Query(new NotificaQueryGetByIdPersistence(new NotificaQueryGetById(
           request.AuthenticationInfo,
           request.IdNotifica
        )), ct);

        if (notifica == null)
        {
            var msg = string.Format("Non esiste la notifica con codice: {0}", request.IdNotifica);
            _logger.LogError(msg);
            throw new DomainException(msg);
        }

        using var cz = await _fattureFactory.Create(cancellationToken: ct);
        var contestazione = await cz.Query(new ContestazioneQueryGetByIdNotificaPersistence(new ContestazioneQueryGetByIdNotifica(
        request.AuthenticationInfo,
        request.IdNotifica
        )), ct);

        var annoNotifica = Convert.ToInt16(notifica.Anno);
        var meseNotifica = Convert.ToInt16(notifica.Mese);

        var calendario = await _handler.Send(new CalendarioContestazioneQueryGet(request.AuthenticationInfo, annoNotifica, meseNotifica));

        bool? chiusuraPermessa = null;
        if (notifica.StatoContestazione == (short)StatoContestazione.NonContestata)
            chiusuraPermessa = false;
        else if (notifica.StatoContestazione == (short)StatoContestazione.Annullata)
            chiusuraPermessa = false;
        else if (notifica.StatoContestazione == (short)StatoContestazione.ContestataEnte)
            chiusuraPermessa = false;
        else if (notifica.StatoContestazione == (short)StatoContestazione.RispostaRecapitista
             || notifica.StatoContestazione == (short)StatoContestazione.RispostaConsolidatore
             || notifica.StatoContestazione == (short)StatoContestazione.RispostaSend)
            chiusuraPermessa = true;
        else if (notifica.StatoContestazione == (short)StatoContestazione.RispostaEnte)
            chiusuraPermessa = true;
        else if (notifica.StatoContestazione == (short)StatoContestazione.Accettata)
            chiusuraPermessa = false;
        else if (notifica.StatoContestazione == (short)StatoContestazione.Chiusa)
            chiusuraPermessa = false;
        else
        {
            var msg = string.Format("Non esiste stato valido associato a notifica con codice: {0}", request.IdNotifica);
            _logger.LogError(msg);
            throw new DomainException(msg);
        }

        return new AzioneNotificaDto()
        {
            ChiusuraPermessa = chiusuraPermessa!.Value && request.AuthenticationInfo.Ruolo == Ruolo.ADMIN,
            ModificaPermessa = calendario.Valid && request.AuthenticationInfo.Ruolo == Ruolo.ADMIN,
            Contestazione = contestazione,
            Notifica = notifica
        };
    }
}