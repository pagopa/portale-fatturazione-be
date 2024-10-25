using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.CommandHandlers;

public class RelUploadCreateCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<RelUploadCreateCommandHandler> logger) : IRequestHandler<RelUploadCreateCommand, bool?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<RelUploadCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(RelUploadCreateCommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo!;

        if (!(authInfo.Profilo == Profilo.PubblicaAmministrazione
            || authInfo.Profilo == Profilo.GestorePubblicoServizio
            || authInfo.Profilo == Profilo.SocietaControlloPubblico
            || authInfo.Profilo == Profilo.PrestatoreServiziPagamento
            || authInfo.Profilo == Profilo.AssicurazioniIVASS
            || authInfo.Profilo == Profilo.StazioneAppaltanteANAC
            || authInfo.Profilo == Profilo.PartnerTecnologico))
            throw new SecurityException(); //401  

        using var uow = await _factory.Create(true, cancellationToken: ct);
        {
            var id = await uow.Execute(new RelTestataUpdateCommandPersistence(command.Map(), _localizer), ct);
            if (id > 0)
            {
                var rowAffected = await uow.Execute(new RelUploadCreateCommandPersistence(command, _localizer), ct);

                if (rowAffected == 1)
                {
                    uow.Commit();
                    return true;
                }
                else
                    uow.Rollback();
            }
            else
                uow.Rollback();
        }
        throw new DomainException(_localizer["xxx"]);
    }
}