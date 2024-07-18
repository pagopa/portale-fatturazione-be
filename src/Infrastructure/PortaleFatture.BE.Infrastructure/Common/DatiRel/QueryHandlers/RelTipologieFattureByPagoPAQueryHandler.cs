using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelTipologieFattureByPagoPAQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelTipologieFattureByPagoPAQueryHandler> logger) : IRequestHandler<RelTipologieFattureByPagoPA, IEnumerable<string>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelTipologieFattureByPagoPAQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(RelTipologieFattureByPagoPA request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelTipologieFattureByPagoPAPersistence(request), ct);
    }
}