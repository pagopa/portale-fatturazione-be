using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FattureSospeseRelExcelHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureSospeseRelExcelHandler> logger) : IRequestHandler<FattureSospeseRelExcelQuery, List<IEnumerable<FattureRelExcelDto>>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureSospeseRelExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<List<IEnumerable<FattureRelExcelDto>>?> Handle(FattureSospeseRelExcelQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var rel = await rs.Query(new FattureSospeseRelExcelBuilderPersistence(request), ct);

        using var rsn = await _factory.Create(cancellationToken: ct);
        var relno = await rsn.Query(new FattureSospeseNotaNoRelExcelPersistence(request), ct);

        using var rsu = await _factory.Create(cancellationToken: ct);
        var relsu = await rsu.Query(new FattureSospeseUnionRelExcelPersistence(request), ct);

        return new List<IEnumerable<FattureRelExcelDto>> { rel!, relsu!, relno! };
    }
}
