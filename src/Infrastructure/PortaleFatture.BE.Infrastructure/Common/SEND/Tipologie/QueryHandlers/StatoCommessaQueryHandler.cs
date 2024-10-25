using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.QueryHandlers;

internal class StatoCommessaQueryHandler : IRequestHandler<StatoCommessaQueryGetByDefault, StatoCommessa?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<StatoCommessaQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public StatoCommessaQueryHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<StatoCommessaQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<StatoCommessa?> Handle(StatoCommessaQueryGetByDefault request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new StatoCommessaQueryGetByDefaultPersistence(), ct);
    }
}