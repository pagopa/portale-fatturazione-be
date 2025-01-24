using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.QueryHandlers;

public class RelMesiByIdEnteHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelMesiByIdEnteHandler> logger) : IRequestHandler<RelMesiByIdEnteQuery, IEnumerable<string>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<RelMesiByIdEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(RelMesiByIdEnteQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelMesiByIdEnteQueryPersistence(request), ct);
    }
}