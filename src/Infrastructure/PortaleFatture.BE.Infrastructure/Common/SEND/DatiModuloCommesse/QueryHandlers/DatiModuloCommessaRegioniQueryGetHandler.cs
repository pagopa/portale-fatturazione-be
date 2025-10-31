using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;
 
public class DatiModuloCommessaRegioniQueryGetHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<DatiModuloCommessaRegioniQueryGetHandler> logger) : IRequestHandler<DatiRegioniModuloCommessaQueryGet, IEnumerable<ModuloCommessaRegioneDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<DatiModuloCommessaRegioniQueryGetHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<ModuloCommessaRegioneDto>?> Handle(DatiRegioniModuloCommessaQueryGet command, CancellationToken ct)
    { 
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiModuloCommessaRegioniQueryGetPersistence(), ct);
    }
}