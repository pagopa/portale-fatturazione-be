using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.QueryHandlers;

public sealed class PSPsQuartersRequestQueryHandler : IRequestHandler<PSPsQuartersRequestQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<PSPsQuartersRequestQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public PSPsQuartersRequestQueryHandler(
      IFattureDbContextFactory factory,
      IStringLocalizer<Localization> localizer,
      ILogger<PSPsQuartersRequestQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(PSPsQuartersRequestQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new PSPsQuartersRequestQueryPersistence(command), ct);
    }
}