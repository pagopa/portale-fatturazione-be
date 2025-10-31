using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;

public class DatiPrevisionaleCalendarioQueryGetHandler
(
IFattureDbContextFactory factory,
IStringLocalizer<Localization> localizer,
ILogger<DatiPrevisionaleCalendarioQueryGetHandler> logger) : IRequestHandler<DatiPrevisionaleCalendarioQuery, IEnumerable<DatiPrevisionaleCalendarioDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiPrevisionaleCalendarioQueryGetHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<DatiPrevisionaleCalendarioDto>> Handle(DatiPrevisionaleCalendarioQuery request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiPrevisionaleCalendarioQueryGetPersistence(request), ct);
    }
}