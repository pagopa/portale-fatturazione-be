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
/// Handler per la query ReportAndamentoCreditoSospesoQuery, responsabile di gestire la logica di business per ottenere il report di andamento del credito sospeso.
/// </summary>
/// <param name="factory">Factory per creare il contesto del database.</param>
/// <param name="localizer"></param>
/// <param name="logger">Logger per registrare le informazioni di esecuzione.</param>
public class ReportAndamentoCreditoSospesoQueryHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<ReportAndamentoCreditoSospesoQueryHandler> logger)
    : IRequestHandler<ReportAndamentoCreditoSospesoQuery, IEnumerable<ReportAndamentoCreditoSospesoDto>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportAndamentoCreditoSospesoQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    /// <summary>
    /// Gestisce la query ReportAndamentoCreditoSospesoQuery, eseguendo la logica di business per ottenere il report di andamento del credito sospeso. Utilizza il contesto del database per eseguire la query di persistenza e restituisce i risultati.
    /// </summary>
    /// <param name="request">La query contenente i parametri per filtrare i dati.</param>
    /// <param name="ct">Token di cancellazione per gestire l'annullamento dell'operazione asincrona.</param>
    /// <returns>Una collezione di oggetti ReportAndamentoCreditoSospesoDto contenenti i dati del report.</returns>
    public async Task<IEnumerable<ReportAndamentoCreditoSospesoDto>?> Handle(
        ReportAndamentoCreditoSospesoQuery request,
        CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ReportAndamentoCreditoSospesoQueryPersistence(request), ct);
    }
}