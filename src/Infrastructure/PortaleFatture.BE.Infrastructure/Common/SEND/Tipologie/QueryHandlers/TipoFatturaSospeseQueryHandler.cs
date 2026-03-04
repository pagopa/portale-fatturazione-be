using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.QueryHandlers;

public class TipoFatturaSospeseQueryHandler : IRequestHandler<TipoFatturaSospeseQueryGetAll, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<TipoFatturaSospeseQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public TipoFatturaSospeseQueryHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<TipoFatturaSospeseQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(TipoFatturaSospeseQueryGetAll command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new TipoFatturaSospeseQueryGetAllPersistence(command.Anno, command.Mese, command.Cancellata!.Value), ct);
    }
}
