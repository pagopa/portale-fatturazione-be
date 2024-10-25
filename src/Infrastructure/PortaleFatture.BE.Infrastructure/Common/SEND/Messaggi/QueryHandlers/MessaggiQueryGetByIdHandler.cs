using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.QueryHandlers;

public class MessaggiQueryGetByIdHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<MessaggiQueryGetByIdHandler> logger) : IRequestHandler<MessaggioQueryGetById, MessaggioDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<MessaggiQueryGetByIdHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<MessaggioDto?> Handle(MessaggioQueryGetById request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new MessaggioQueryGetByIdPersistence(request), ct);
    }
}