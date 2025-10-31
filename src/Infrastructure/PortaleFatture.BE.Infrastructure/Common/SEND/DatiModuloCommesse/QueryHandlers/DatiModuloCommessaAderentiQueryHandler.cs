using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers; 
 
public class DatiModuloCommessaAderentiQueryHandler : IRequestHandler<DatiModuloCommessaAderentiQueryGet, DatiModuloCommessaAderentiDto?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiModuloCommessaAderentiQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public DatiModuloCommessaAderentiQueryHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<DatiModuloCommessaAderentiQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiModuloCommessaAderentiDto?> Handle(DatiModuloCommessaAderentiQueryGet request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiModuloCommessaAderentiQueryGetPersistence(idEnte: request.IdEnte), ct);
    }
}