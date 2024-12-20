using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.QueryHandlers;
public sealed class KPIPagamentiScontoQueryHandler(
  IFattureDbContextFactory factory,
  IStringLocalizer<Localization> localizer,
  ILogger<KPIPagamentiScontoQueryHandler> logger) : IRequestHandler<KPIPagamentiScontoQuery, GridKPIPagamentiScontoReportListDto>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<KPIPagamentiScontoQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<GridKPIPagamentiScontoReportListDto> Handle(KPIPagamentiScontoQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new KPIPagamentiScontoQueryPersistence(command), ct);
    }
}