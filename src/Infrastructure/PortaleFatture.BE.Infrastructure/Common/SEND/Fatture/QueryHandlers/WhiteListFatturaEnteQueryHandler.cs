using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class WhiteListFatturaEnteQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<WhiteListFatturaEnteQueryHandler> logger) : IRequestHandler<WhiteListFatturaEnteQuery, WhiteListFatturaEnteDto?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<WhiteListFatturaEnteQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<WhiteListFatturaEnteDto?> Handle(WhiteListFatturaEnteQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new WhiteListFatturaEnteQueryPersistence(request), ct);
    }
}