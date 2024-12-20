using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.KPIPagamenti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.QueryHandlers;

public sealed class FinancialReportQueryGetKPMGReportExcelHandler(
  IFattureDbContextFactory factory,
  IStringLocalizer<Localization> localizer,
  ILogger<FinancialReportQueryGetKPMGReportExcelHandler> logger) : IRequestHandler<FinancialReportQueryGetKPMGReportExcel, KPMGReportListDto>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FinancialReportQueryGetKPMGReportExcelHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<KPMGReportListDto> Handle(FinancialReportQueryGetKPMGReportExcel command, CancellationToken ct)
    {
        KPMGReportListDto report = new();

        using var uow = await _factory.Create(true, cancellationToken: ct);
        var financials = await uow.Query(new FinancialReportQueryGetFinancialReportExcelPersistence(command.Map()), ct);
        var kpmgs = await uow.Query(new FinancialReportQueryGetKPMGReportExcelPersistence(command), ct); 
        var sconti = await uow.Query(new KPIPagamentiScontoKPMGQueryPersistence(command.Mapv2()), ct);
        var scontiLista = await uow.Query(new KPIPagamentiScontoKPMGListQueryPersistence(command.Mapv2()), ct);
        report.FinancialReports = financials;
        report.KPMGReports = kpmgs;
        report.Sconti = sconti;
        report.ScontiLista = scontiLista;
        return report;
    }
}