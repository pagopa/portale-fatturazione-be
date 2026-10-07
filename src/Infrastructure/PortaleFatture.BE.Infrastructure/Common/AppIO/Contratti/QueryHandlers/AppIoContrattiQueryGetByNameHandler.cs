using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.QueryHandlers;

/// <summary>
/// Gestisce la ricerca dei contratti APP IO per nome delegando ad <see cref="AppIoContrattiQueryGetByNamePersistence"/>.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoContrattiQueryGetByNameHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoContrattiQueryGetByName, IEnumerable<AppIoContrattoNome>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Esegue la ricerca dei contratti APP IO per nome dell'ente.
    /// </summary>
    /// <param name="command">La query con il nome e i trimestri.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I contratti il cui nome contiene il testo cercato.</returns>
    public async Task<IEnumerable<AppIoContrattoNome>> Handle(AppIoContrattiQueryGetByName command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoContrattiQueryGetByNamePersistence(command), ct);
    }
}
