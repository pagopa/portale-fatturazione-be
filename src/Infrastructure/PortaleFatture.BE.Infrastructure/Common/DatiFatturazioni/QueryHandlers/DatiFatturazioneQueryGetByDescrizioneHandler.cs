using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.QueryHandlers;

public class DatiFatturazioneQueryGetByDescrizioneHandler : IRequestHandler<DatiFatturazioneQueryGetByDescrizione, IEnumerable<DatiFatturazioneEnteDto>?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<DatiFatturazioneQueryGetByDescrizioneHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;
    private readonly IPortaleFattureOptions _options;
    public DatiFatturazioneQueryGetByDescrizioneHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         IPortaleFattureOptions options,
         ILogger<DatiFatturazioneQueryGetByDescrizioneHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
        _options = options;
    }

    public async Task<IEnumerable<DatiFatturazioneEnteDto>?> Handle(DatiFatturazioneQueryGetByDescrizione command, CancellationToken ct)
    {
        if (command.AuthenticationInfo!.Auth! != AuthType.PAGOPA)
            throw new SecurityException();

        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiFatturazioneQueryGetByDescrizionePersistence(_options, 
            command.IdEnti,
            command.Prodotto,
            command.Profilo,
            command.Top), ct); 
    }
}