using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Fattura;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
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
        IEnumerable<FattureCommessaExcelDto>? fTotale;
        IEnumerable<FattureCommessaExcelDto>? fEliminateCommessa;
        using (var rs = await _factory.Create(true, cancellationToken: ct))
        {
            fTotale = await rs.Query(new FattureModuloCommessaExcelPersistence(request), ct);
            fEliminateCommessa = await rs.Query(new FattureModuloCommessaEliminateExcelPersistence(request.Map()), ct);
        } 

        var fStimate = from p in fTotale where p.FkIdStato == FatturaExtensions.ChiusaStimato select p;
        var fFattureStimate = from p in fTotale where p.FkIdStato == FatturaExtensions.ChiusaStimato && p.IdFattura is not null select p;
        var fFatture = from p in fTotale where p.IdFattura is not null select p;
        var fModuli = from p in fTotale where p.IdFattura is null select p;
        fTotale = from q in fTotale orderby q.TotaleFattura descending select q;
        return [fTotale!, fStimate!, fFattureStimate!, fFatture!, fModuli!, fEliminateCommessa!];
    }
}