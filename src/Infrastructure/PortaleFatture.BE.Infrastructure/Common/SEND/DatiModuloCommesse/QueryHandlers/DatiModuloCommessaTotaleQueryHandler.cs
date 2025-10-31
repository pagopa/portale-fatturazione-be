using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;


public class DatiModuloCommessaTotaleQueryHandler
(
IFattureDbContextFactory factory,
IStringLocalizer<Localization> localizer,
ILogger<DatiModuloCommessaTotaleQueryHandler> logger) : IRequestHandler<DatiModuloCommessaTotaleQueryGet, IEnumerable<DatiModuloCommessaTotale>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiModuloCommessaTotaleQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<DatiModuloCommessaTotale>?> Handle(DatiModuloCommessaTotaleQueryGet request, CancellationToken ct)
    {
        var idEnte = request.AuthenticationInfo.IdEnte;
        var prodotto = request.AuthenticationInfo.Prodotto;
        var anno = request.AnnoValidita;
        var mese = request.MeseValidita;    
        using var uow = await _factory.Create(cancellationToken: ct);
        var result = await uow.Query(new DatiModuloCommessaTotaleQueryGetByIdPersistence(idEnte, anno, mese, prodotto), ct);
        return result;
    }
}