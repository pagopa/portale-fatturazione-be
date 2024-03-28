using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.QueryHandlers;

public class RelTestataQueryGetByIdEnteHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelTestataQueryGetByIdEnteHandler> logger) : IRequestHandler<RelTestataQueryGetByIdEnte, RelTestataDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelTestataQueryGetByIdEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<RelTestataDto?> Handle(RelTestataQueryGetByIdEnte request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelTestataQueryGetByIdEntePersistence(request), ct);
    }
}