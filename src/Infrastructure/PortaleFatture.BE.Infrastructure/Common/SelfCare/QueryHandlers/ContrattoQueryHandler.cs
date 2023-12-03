using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SelfCare.QueryHandlers;

public class ContrattoQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<ContrattoQueryHandler> logger) : IRequestHandler<ContrattoQueryGetById, Contratto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ContrattoQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<Contratto?> Handle(ContrattoQueryGetById request, CancellationToken ct)
    {
        var idEnte = request.AuthenticationInfo!.IdEnte;
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new ContrattoQueryGetByIdPersistence(idEnte!), ct);
    }
}