using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.QueryHandlers;

public class ContrattoQueryHandler : IRequestHandler<ContrattoQueryGetById, Contratto?>
{
    private readonly ISelfCareDbContextFactory _factory;
    private readonly ILogger<ContrattoQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public ContrattoQueryHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<ContrattoQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }
    public async Task<Contratto?> Handle(ContrattoQueryGetById request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new ContrattoQueryGetByIdPersistence(request.IdEnte!), ct);
    }
}