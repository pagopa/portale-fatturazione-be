using Microsoft.Extensions.Localization;
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

public class DatiFatturazioneQueryGetByIdEnteHandler : IRequestHandler<DatiFatturazioneQueryGetByIdEnte, DatiFatturazione?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiFatturazioneQueryGetByIdEnteHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    public DatiFatturazioneQueryGetByIdEnteHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiFatturazioneQueryGetByIdEnteHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<DatiFatturazione?> Handle(DatiFatturazioneQueryGetByIdEnte command, CancellationToken ct)
    {
        var idEnte = command.AuthenticationInfo!.IdEnte!;
        using var uow = await _factory.Create(true, cancellationToken: ct);
        var datiCommessa = await uow.Query(new DatiFatturazioneQueryGetByIdEntePersistence(idEnte), ct) ?? throw new NotFoundException(_localizer["DatiFatturazioneGetErrorIdEnte", idEnte]);
        datiCommessa.Contatti = await uow.Query(new DatiFatturazioneContattoQueryGetByIdPersistence(datiCommessa.Id), ct);
        return datiCommessa;
    }
}