using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SelfCare;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.QueryHandlers;

public class EnteQueryGetByRecapitistiConsolidatoriHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<EnteQueryGetByRecapitistiConsolidatoriHandler> logger) : IRequestHandler<EnteQueryGetByRecapitistiConsolidatori, IEnumerable<Ente>>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<EnteQueryGetByRecapitistiConsolidatoriHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<Ente>> Handle(EnteQueryGetByRecapitistiConsolidatori request, CancellationToken ct)
    {
        if (request.AuthenticationInfo!.Auth! != AuthType.PAGOPA)
            throw new SecurityException();

        if (request == null || request!.Tipo == null || (request!.Tipo != Profilo.Recapitista && request!.Tipo != Profilo.Consolidatore))
            throw new ValidationException(_localizer["RicercaEnteEmpty"]);

        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new EnteQueryGetByRecapitistiConsolidatoriPersistence(request.Tipo), ct);
    }
}