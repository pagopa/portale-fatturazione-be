﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.QueryHandlers;

public class SpedizioneQueryHandler : IRequestHandler<SpedizioneQueryGetAll, IEnumerable<CategoriaSpedizione>?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<SpedizioneQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public SpedizioneQueryHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<SpedizioneQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoriaSpedizione>?> Handle(SpedizioneQueryGetAll request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new SpedizioneQueryGetAllPersistence(), ct);
    }
}