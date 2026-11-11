using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.QueryHandlers;

/// <summary>
/// Gestisce la lettura dei trimestri dei documenti emessi APP IO.
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
/// Gestisce la lettura degli anni dei documenti emessi APP IO.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportYearsQueryHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportYearsQuery, IEnumerable<int>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce gli anni distinti, dal più recente.
    /// </summary>
    /// <param name="command">La query.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>Gli anni.</returns>
    public async Task<IEnumerable<int>> Handle(AppIoFinancialReportYearsQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoFinancialReportYearsQueryPersistence(command), ct);
    }
}

/// <summary>
/// Gestisce la lettura dei codici fiscali degli enti APP IO.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportCodiciFiscaliQueryHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportCodiciFiscaliQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce i codici fiscali distinti, in ordine crescente.
    /// </summary>
    /// <param name="command">La query.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I codici fiscali.</returns>
    public async Task<IEnumerable<string>> Handle(AppIoFinancialReportCodiciFiscaliQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoFinancialReportCodiciFiscaliQueryPersistence(command), ct);
    }
}

/// <summary>
/// Gestisce la griglia dei documenti emessi APP IO: legge le righe e le raggruppa per contratto e trimestre.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQueryGetByRicercaHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQueryGetByRicerca, AppIoFinancialReportListDto>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce le righe padre che soddisfano il filtro, con le righe dei documenti.
    /// </summary>
    /// <param name="command">La query con il filtro.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>Le righe padre e il loro numero.</returns>
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
/// Gestisce il dettaglio di un documento emesso APP IO (contratto e trimestre).
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoFinancialReportQueryGetDettaglioHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoFinancialReportQueryGetDettaglio, IEnumerable<AppIoFinancialReportDettaglioRigaDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce le righe del dettaglio.
    /// </summary>
    /// <param name="command">La query con contratto e trimestre.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>Le righe del dettaglio, ordinate per documento e articolo.</returns>
    public async Task<IEnumerable<AppIoFinancialReportDettaglioRigaDto>> Handle(AppIoFinancialReportQueryGetDettaglio command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoFinancialReportDettaglioQueryPersistence(command), ct);
    }
}
