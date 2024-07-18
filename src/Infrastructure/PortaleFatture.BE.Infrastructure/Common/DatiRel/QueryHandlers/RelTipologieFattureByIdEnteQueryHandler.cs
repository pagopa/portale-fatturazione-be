﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;

public class RelTipologieFattureByIdEnteQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelTipologieFattureByIdEnteQueryHandler> logger) : IRequestHandler<RelTipologieFattureByIdEnte, IEnumerable<string>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelTipologieFattureByIdEnteQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<string>?> Handle(RelTipologieFattureByIdEnte request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelTipologieFattureByIdEntePersistence(request), ct);
    }
}