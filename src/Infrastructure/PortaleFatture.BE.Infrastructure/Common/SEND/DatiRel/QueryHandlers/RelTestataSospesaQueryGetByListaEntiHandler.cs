using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.QueryHandlers;

public class RelTestataSospesaQueryGetByListaEntiHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelTestataSospesaQueryGetByListaEntiHandler> logger) : IRequestHandler<RelTestataSospesaQueryGetByListaEnti, RelTestataSospesaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelTestataSospesaQueryGetByListaEntiHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<RelTestataSospesaDto?> Handle(RelTestataSospesaQueryGetByListaEnti request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelTestataSospesaQueryGetByListaEntiPersistence(request), ct);
    }
}