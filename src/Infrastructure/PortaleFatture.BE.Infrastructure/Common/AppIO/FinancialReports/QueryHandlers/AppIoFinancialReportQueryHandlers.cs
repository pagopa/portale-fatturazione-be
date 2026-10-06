using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.QueryHandlers;

/// <summary>
/// Gestisce la lettura dei trimestri dei documenti contabili APP IO.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQuartersQueryHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQuartersQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce i trimestri distinti, dal più recente.
    /// </summary>
    /// <param name="command">La query con l'anno facoltativo.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I trimestri nel formato 'AAAA_T'.</returns>
    public async Task<IEnumerable<string>> Handle(AppIoFinancialReportQuartersQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoFinancialReportQuartersQueryPersistence(command), ct);
    }
}

/// <summary>
/// Gestisce la griglia dei documenti contabili APP IO: legge le righe e le raggruppa per documento.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQueryGetByRicercaHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQueryGetByRicerca, AppIoFinancialReportListDto>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce i documenti che soddisfano il filtro, con le loro posizioni.
    /// </summary>
    /// <param name="command">La query con il filtro.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I documenti e il loro numero.</returns>
    public async Task<AppIoFinancialReportListDto> Handle(AppIoFinancialReportQueryGetByRicerca command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var righe = await uow.Query(new AppIoFinancialReportRigheQueryPersistence(command), ct);
        return righe.Raggruppa();
    }
}

/// <summary>
/// Gestisce il download "Documenti Contabili" APP IO: le righe così come sono, senza raggruppare.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQueryGetByRicercaExcelHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQueryGetByRicercaExcel, IEnumerable<AppIoFinancialReportRigaDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce le righe che soddisfano il filtro.
    /// </summary>
    /// <param name="command">La query con il filtro.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>Le righe dei documenti.</returns>
    public async Task<IEnumerable<AppIoFinancialReportRigaDto>> Handle(AppIoFinancialReportQueryGetByRicercaExcel command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoFinancialReportRigheQueryPersistence(command), ct);
    }
}

/// <summary>
/// Gestisce il download "Financial Report" APP IO: righe del financial report e posizioni, con lo
/// stesso filtro, nella stessa unit of work.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQueryGetDettaglioExcelHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQueryGetDettaglioExcel, AppIoFinancialReportDettaglioDto>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce le righe del financial report e le posizioni che soddisfano il filtro.
    /// </summary>
    /// <param name="command">La query con il filtro.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I due insiemi di righe.</returns>
    public async Task<AppIoFinancialReportDettaglioDto> Handle(AppIoFinancialReportQueryGetDettaglioExcel command, CancellationToken ct)
    {
        // Create(true): senza transazione la connessione si chiude dopo la prima query, e la seconda
        // fallisce con "ConnectionString non inizializzata" (come nel gemello pagoPA)
        using var uow = await _factory.Create(true, cancellationToken: ct);
        return new AppIoFinancialReportDettaglioDto
        {
            FinancialReports = await uow.Query(new AppIoFinancialReportValoriQueryPersistence(command), ct),
            Posizioni = await uow.Query(new AppIoFinancialReportPosizioniQueryPersistence(command), ct)
        };
    }
}
