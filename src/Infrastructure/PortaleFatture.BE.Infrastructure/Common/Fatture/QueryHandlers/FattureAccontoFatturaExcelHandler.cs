using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureAccontoFatturaExcelHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureAccontoFatturaExcelHandler> logger) : IRequestHandler<FattureAccontoExcelQuery, List<IEnumerable<FattureAccontoExcelDto>>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureAccontoFatturaExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<List<IEnumerable<FattureAccontoExcelDto>>?> Handle(FattureAccontoExcelQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var fTotale = await rs.Query(new FattureAccontoFatturaExcelBuilderPersistence(request), ct); 
        return new List<IEnumerable<FattureAccontoExcelDto>> { fTotale!};
    }
}