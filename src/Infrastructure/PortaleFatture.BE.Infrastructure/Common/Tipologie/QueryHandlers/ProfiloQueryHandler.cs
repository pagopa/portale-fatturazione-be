using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.QueryHandlers;

public class ProfiloQueryHandler : IRequestHandler<ProfiloQueryGetAll, IEnumerable<string>>
{
    private readonly ISelfCareDbContextFactory _factory;
    private readonly ILogger<ProfiloQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public ProfiloQueryHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<ProfiloQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(ProfiloQueryGetAll request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new ProfiloQueryGetAllPersistence(), ct);
    }
}