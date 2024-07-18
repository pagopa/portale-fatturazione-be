using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.QueryHandlers;

public class TipoFatturaQueryHandler : IRequestHandler<TipoFatturaQueryGetAll, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<TipoFatturaQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public TipoFatturaQueryHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<TipoFatturaQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(TipoFatturaQueryGetAll request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new TipoFatturaQueryGetAllPersistence(request.Anno, request.Mese), ct);
    }
}