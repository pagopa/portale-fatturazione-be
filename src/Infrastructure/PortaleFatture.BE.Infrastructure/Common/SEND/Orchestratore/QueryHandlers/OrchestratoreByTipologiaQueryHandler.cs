using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Orchestratore.QueryHandlers;

public class OrchestratoreByTipologiaQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<OrchestratoreByTipologiaQueryHandler> logger) : IRequestHandler<OrchestratoreByTipologiaQuery, IEnumerable<string>>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<OrchestratoreByTipologiaQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>> Handle(OrchestratoreByTipologiaQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new OrchestratoreByTipologiaQueryPersistence(request), ct);
    }
}