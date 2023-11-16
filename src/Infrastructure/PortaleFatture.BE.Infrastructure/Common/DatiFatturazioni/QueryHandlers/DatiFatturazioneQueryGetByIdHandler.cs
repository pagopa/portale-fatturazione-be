﻿using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiFatturazioni;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using MediatR;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.QueryHandlers;

public class DatiFatturazioneQueryGetByIdHandler : IRequestHandler<DatiFatturazioneQueryGetById, DatiFatturazione>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiFatturazioneQueryGetByIdHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiFatturazioneQueryGetByIdHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiFatturazioneQueryGetByIdHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiFatturazione> Handle(DatiFatturazioneQueryGetById command, CancellationToken ct)
    {
        using var uow = await _factory.Create(true, cancellationToken: ct);
        var datiCommessa = await uow.Query(new DatiFatturazioneWithContattiQueryGetByIdPersistence(command.Id), ct);
        return datiCommessa is null ? throw new NotFoundException(_localizer["DatiFatturazioneGetError", command.Id]) : datiCommessa;
    }
} 