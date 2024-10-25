using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;

public class DatiConfigurazioneModuloCommessaQueryHandler : IRequestHandler<DatiConfigurazioneModuloCommessaQueryGet, DatiConfigurazioneModuloCommessa?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiConfigurazioneModuloCommessaQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public DatiConfigurazioneModuloCommessaQueryHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiConfigurazioneModuloCommessaQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiConfigurazioneModuloCommessa?> Handle(DatiConfigurazioneModuloCommessaQueryGet request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiConfigurazioneModuloCommessaQueryGetPersistence(prodotto: request.Prodotto, idTipoContratto: request.IdTipoContratto), ct);
    }
}