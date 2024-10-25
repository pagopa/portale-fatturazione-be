using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.QueryHandlers;

public sealed class FinancialReportQuartersQueryHandler : IRequestHandler<FinancialReportQuartersQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<FinancialReportQuartersQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public FinancialReportQuartersQueryHandler(
      IFattureDbContextFactory factory,
      IStringLocalizer<Localization> localizer,
      ILogger<FinancialReportQuartersQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> Handle(FinancialReportQuartersQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new FinancialReportQuartersQueryPersistence(command), ct);
    }
}