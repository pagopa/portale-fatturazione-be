using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

 
public class AnniMesiTipologiaByEnteHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<AnniMesiTipologiaByEnteHandler> logger) : IRequestHandler<AnniMesiTipologiaByEnteQuery, IEnumerable<AnniMesiTipologiaByEnteDto>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<AnniMesiTipologiaByEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<AnniMesiTipologiaByEnteDto>?> Handle(AnniMesiTipologiaByEnteQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new AnniMesiTipologiaByEnteQueryPersistence(request), ct);
    }
}