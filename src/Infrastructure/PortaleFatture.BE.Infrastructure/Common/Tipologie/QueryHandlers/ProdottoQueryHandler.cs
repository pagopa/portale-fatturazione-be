using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Tipologie.QueryHandlers;

public class ProdottoQueryHandler : IRequestHandler<ProdottoQueryGetAll, IEnumerable<Prodotto>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<ProdottoQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public ProdottoQueryHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<ProdottoQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<Prodotto>> Handle(ProdottoQueryGetAll request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new ProdottoQueryGetAllPersistence(), ct);
    }
}