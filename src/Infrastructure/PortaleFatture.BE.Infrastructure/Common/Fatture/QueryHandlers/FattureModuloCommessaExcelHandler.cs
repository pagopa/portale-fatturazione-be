using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Core.Entities.Fattura;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureModuloCommessaExcelHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureModuloCommessaExcelHandler> logger) : IRequestHandler<FattureCommessaExcelQuery, List<IEnumerable<FattureCommessaExcelDto>>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureModuloCommessaExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<List<IEnumerable<FattureCommessaExcelDto>>?> Handle(FattureCommessaExcelQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        var fTotale = await rs.Query(new FattureModuloCommessaExcelBuilderPersistence(request), ct);
        var fStimate = from p in fTotale where (p.FkIdStato == FatturaExtensions.ChiusaStimato) select p;
        var fFattureStimate = from p in fTotale where (p.FkIdStato == FatturaExtensions.ChiusaStimato && p.IdFattura is not null) select p;
        var fFatture = from p in fTotale where (p.IdFattura is not null) select p;
        var fModuli = from p in fTotale where (p.IdFattura is null) select p;
        fTotale = from q in fTotale orderby q.TotaleFattura descending select q;
        return new List<IEnumerable<FattureCommessaExcelDto>> { fTotale!, fStimate!, fFattureStimate!, fFatture!, fModuli! };
    }
}