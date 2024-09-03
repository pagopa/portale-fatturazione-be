using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Common.Messaggi.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Notifiche.QueryHandlers;

public class MessaggiQueryGetByIdUtenteHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer, 
     ILogger<MessaggiQueryGetByIdUtenteHandler> logger) : IRequestHandler<MessaggiQueryGetByIdUtente, MessaggioListaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<MessaggiQueryGetByIdUtenteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    public async Task<MessaggioListaDto?> Handle(MessaggiQueryGetByIdUtente request, CancellationToken ct)
    { 
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new MessaggiQueryGetByIdUtentePersistence(request), ct); 
    }
}