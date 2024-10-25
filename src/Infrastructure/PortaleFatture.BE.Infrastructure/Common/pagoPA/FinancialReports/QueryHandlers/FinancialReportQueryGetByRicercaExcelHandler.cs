using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.QueryHandlers;

public sealed class FinancialReportQueryGetByRicercaExcelHandler : IRequestHandler<FinancialReportQueryGetByRicercaExcel, IEnumerable<GridFinancialReportDto>>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<FinancialReportQueryGetByRicercaExcel> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public FinancialReportQueryGetByRicercaExcelHandler(
      IFattureDbContextFactory factory,
      IStringLocalizer<Localization> localizer,
      ILogger<FinancialReportQueryGetByRicercaExcel> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IEnumerable<GridFinancialReportDto>> Handle(FinancialReportQueryGetByRicercaExcel command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new FinancialReportQueryGetByRicercaExcelPersistence(command), ct);
    }
}