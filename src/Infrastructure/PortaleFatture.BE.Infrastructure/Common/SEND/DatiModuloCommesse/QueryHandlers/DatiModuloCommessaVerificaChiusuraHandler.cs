using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers;
 

public class DatiModuloCommessaVerificaChiusuraHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<DatiModuloCommessaVerificaChiusuraHandler> logger) : IRequestHandler<DatiModuloCommessaVerificaChiusura, bool>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<DatiModuloCommessaVerificaChiusuraHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool> Handle(DatiModuloCommessaVerificaChiusura request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new DatiModuloCommessaVerificaChiusuraPersistence( anno: request.Anno, mese: request.Mese), ct);
    }
}