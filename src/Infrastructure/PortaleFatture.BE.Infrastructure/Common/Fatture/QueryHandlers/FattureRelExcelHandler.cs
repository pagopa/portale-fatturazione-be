using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureRelExcelHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureRelExcelHandler> logger) : IRequestHandler<FattureRelExcelQuery, List<IEnumerable<FattureRelExcelDto>>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureRelExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<List<IEnumerable<FattureRelExcelDto>>?> Handle(FattureRelExcelQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var rel = await rs.Query(new FattureRelExcelBuilderPersistence(request), ct);

        using var rsn = await _factory.Create(cancellationToken: ct);
        var relno = await rsn.Query(new FattureNotaNoRelExcelBuilderPersistence(request), ct);

        using var rsu = await _factory.Create(cancellationToken: ct);
        var relsu = await rsu.Query(new FattureUnionRelExcelBuilderPersistence(request), ct);

        return new List<IEnumerable<FattureRelExcelDto>> { rel!, relsu!, relno! };
    }
}