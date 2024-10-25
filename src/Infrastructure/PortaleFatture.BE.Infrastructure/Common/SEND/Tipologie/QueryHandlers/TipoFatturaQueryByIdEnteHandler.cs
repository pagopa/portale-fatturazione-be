using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.QueryHandlers;

public class TipoFatturaQueryByIdEnteHandler : IRequestHandler<TipoFatturaQueryGetAllByIdEnte, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<TipoFatturaQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public TipoFatturaQueryByIdEnteHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<TipoFatturaQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(TipoFatturaQueryGetAllByIdEnte request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new TipoFatturaQueryGetAllByIdEntePersistence(request.AuthenticationInfo!.IdEnte, request.Anno, request.Mese), ct);
    }
}