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

public class EntiQueryGetByDescrizioneHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<EntiQueryGetByDescrizioneHandler> logger) : IRequestHandler<EnteQueryGetByDescrizione, IEnumerable<Ente>>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<EntiQueryGetByDescrizioneHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<Ente>> Handle(EnteQueryGetByDescrizione request, CancellationToken ct)
    {
        //if (request.AuthenticationInfo!.Auth! != AuthType.PAGOPA || request.AuthenticationInfo!.Auth! != AuthType.con)
        //    throw new SecurityException();

        if (request == null || request!.Descrizione == null || request!.Descrizione.Length < 3)
            throw new ValidationException(_localizer["RicercaEnteEmpty"]);

        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new EnteQueryGetByDescrizionePersistence(request.Descrizione!), ct);
    }
}