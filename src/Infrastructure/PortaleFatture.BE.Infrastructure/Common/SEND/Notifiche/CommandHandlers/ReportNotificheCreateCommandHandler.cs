using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.CommandHandlers;

public class ReportNotificheCreateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheCreateCommandHandler> logger) : IRequestHandler<ReportNotificheCreateCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<int?> Handle(ReportNotificheCreateCommand command, CancellationToken ct)
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

        using var uow = await _factory.Create(true, cancellationToken: ct);
        var idReport = await uow.Execute<int>(new ReportNotificheCreateCommandPersistence(command, _localizer), ct);
        if (idReport > 0)
        { 
            uow.Commit();
            return idReport;
        }
        else
        {
            uow.Rollback();
            return null;
        }            
    }
}