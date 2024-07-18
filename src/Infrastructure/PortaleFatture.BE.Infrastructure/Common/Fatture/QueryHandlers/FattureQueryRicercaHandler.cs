using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureQueryRicercaHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureQueryRicercaHandler> logger) : IRequestHandler<FattureQueryRicerca, FattureListaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureQueryRicercaHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<FattureListaDto?> Handle(FattureQueryRicerca request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureQueryRicercaPersistence(request), ct);
    }
}