using MediatR;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries;
using PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.QueryHandlers;

/// <summary>
/// Gestisce la lettura dei trimestri dei contratti APP IO delegando ad <see cref="AppIoContrattiQuartersQueryPersistence"/>.
/// </summary>
/// <param name="factory">La factory del contesto database.</param>
public sealed class AppIoContrattiQuartersQueryHandler(IFattureDbContextFactory factory)
    : IRequestHandler<AppIoContrattiQuartersQuery, IEnumerable<string>>
{
    private readonly IFattureDbContextFactory _factory = factory;

    /// <summary>
    /// Restituisce i trimestri distinti dei contratti APP IO.
    /// </summary>
    /// <param name="command">La query con l'anno facoltativo.</param>
    /// <param name="ct">Il token di annullamento.</param>
    /// <returns>I trimestri nel formato 'AAAA_T', in ordine decrescente.</returns>
    public async Task<IEnumerable<string>> Handle(AppIoContrattiQuartersQuery command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new AppIoContrattiQuartersQueryPersistence(command), ct);
    }
}
