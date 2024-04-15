using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.QueryHandlers;

public class NotificaQueryGetByRecapitistaHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     IMediator handler,
     ILogger<NotificaQueryGetByRecapitistaHandler> logger) : IRequestHandler<NotificaQueryGetByRecapitista, NotificaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<NotificaQueryGetByRecapitistaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<NotificaDto?> Handle(NotificaQueryGetByRecapitista request, CancellationToken ct)
    {
        var annoNotifica = request.AnnoValidita!.Value;
        var meseNotifica = request.MeseValidita!.Value;

        var calendario = await _handler.Send(new CalendarioContestazioneQueryGet(request.AuthenticationInfo, annoNotifica, meseNotifica));

        if (!calendario.ValidVisualizzazione)
            return new NotificaDto()
            {
                Count = 0,
                Notifiche = []
            };
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new NotificaQueryGetByRecapitistaPersistence(request), ct); 
    }
}