using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

/// <summary>
/// Query handler per la gestione del report delle fatture.
/// </summary>
/// <param name="factory">Factory per la creazione del contesto del database delle fatture.</param>
/// <param name="localizer">Localizzatore per le risorse di localizzazione.</param>
/// <param name="logger">Logger per la registrazione delle informazioni e degli errori.</param>
public class GestioneFattureReportQueryHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<GestioneFattureReportQueryHandler> logger) : IRequestHandler<GestioneFattureReportQuery, IEnumerable<GestioneFattureReportDto>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<GestioneFattureReportQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<GestioneFattureReportDto>?> Handle(GestioneFattureReportQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new GestioneFattureReportQueryPersistence(request), ct);
    }
}







