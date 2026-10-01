using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.QueryHandlers;

/// <summary>
/// Gestisce la ricerca dei contratti APP IO delegando ad <see cref="AppIoContrattiQueryGetByRicercaPersistence"/>.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoContrattiQueryGetByRicercaHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoContrattiQueryGetByRicerca, AppIoContrattiListDto>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Esegue la ricerca dei contratti APP IO.
    /// </summary>
    /// <param name="command">La query con filtri e paginazione.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I contratti della pagina e il conteggio totale.</returns>
    public async Task<AppIoContrattiListDto> Handle(AppIoContrattiQueryGetByRicerca command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoContrattiQueryGetByRicercaPersistence(command), ct);
    }
}
