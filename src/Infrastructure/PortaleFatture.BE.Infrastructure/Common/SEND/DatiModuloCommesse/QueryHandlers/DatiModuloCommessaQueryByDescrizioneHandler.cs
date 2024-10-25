using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;

public class DatiModuloCommessaQueryByDescrizioneHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<DatiModuloCommessaQueryByDescrizioneHandler> logger) : IRequestHandler<DatiModuloCommessaQueryGetByDescrizione, IEnumerable<ModuloCommessaByRicercaDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<DatiModuloCommessaQueryByDescrizioneHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<ModuloCommessaByRicercaDto>?> Handle(DatiModuloCommessaQueryGetByDescrizione command, CancellationToken ct)
    {
        if (command.MeseValidita == null || command.AnnoValidita == null)
            throw new ArgumentException("Passare un anno e un mese");
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiModuloCommessaQueryGetByDescrizionePersistence(command.IdEnti,
            command.AnnoValidita.Value,
            command.MeseValidita.Value,
            command.Prodotto), ct);
    }
}