using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class RicercaContrattiTipologiaQueryHandler(
 IMediator handler,
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RicercaContrattiTipologiaQueryHandler> logger) : IRequestHandler<RicercaContrattiTipologiaQuery, ContrattiTipologiaDto?>
{
    private readonly IMediator _handler = handler;
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RicercaContrattiTipologiaQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<ContrattiTipologiaDto?> Handle(RicercaContrattiTipologiaQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RicercaContrattiTipologiaPersistence(request), ct); 
    }
}