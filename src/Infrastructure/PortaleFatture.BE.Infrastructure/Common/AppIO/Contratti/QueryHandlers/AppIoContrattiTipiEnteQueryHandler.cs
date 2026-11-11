using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.QueryHandlers;

/// <summary>
/// Gestisce la lettura dei tipi ente APP IO delegando ad <see cref="AppIoContrattiTipiEnteQueryPersistence"/>.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoContrattiTipiEnteQueryHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoContrattiTipiEnteQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce i tipi ente distinti.
    /// </summary>
    /// <param name="command">La query.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I tipi ente, in ordine alfabetico.</returns>
    public async Task<IEnumerable<string>> Handle(AppIoContrattiTipiEnteQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoContrattiTipiEnteQueryPersistence(command), ct);
    }
}
