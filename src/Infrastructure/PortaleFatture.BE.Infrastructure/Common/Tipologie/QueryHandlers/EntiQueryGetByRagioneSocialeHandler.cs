using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.QueryHandlers;

public class EntiQueryGetByRagioneSocialeHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<EntiQueryGetByRagioneSocialeHandler> logger) : IRequestHandler<EnteQueryGetByRagioneSociale, IEnumerable<string>>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<EntiQueryGetByRagioneSocialeHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<string>> Handle(EnteQueryGetByRagioneSociale request, CancellationToken ct)
    {
        if (request.AuthenticationInfo!.Auth! != AuthType.PAGOPA)
            throw new SecurityException();

        if (request == null || request!.Descrizione == null)
            throw new ValidationException(_localizer["RicercaEnteEmpty"]);

        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new EnteQueryGetByRagioneSocialePersistence(request.Descrizione!, request.Prodotto, request.Profilo), ct);
    }
}