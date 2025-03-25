using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.QueryHandlers;

public class OrchestratoreByDateQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<OrchestratoreByDateQueryHandler> logger) : IRequestHandler<OrchestratoreByDateQuery, OrchestratoreDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<OrchestratoreByDateQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<OrchestratoreDto?> Handle(OrchestratoreByDateQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new OrchestratoreByDateQueryPersistence(request), ct);
    }
}