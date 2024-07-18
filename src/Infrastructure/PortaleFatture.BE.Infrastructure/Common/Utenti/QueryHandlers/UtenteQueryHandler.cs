using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Queries;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.QueryHandlers;

public class UtenteQueryHandler : IRequestHandler<UtenteQueryGetById, Utente?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<UtenteQueryHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public UtenteQueryHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<UtenteQueryHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }
    public async Task<Utente?> Handle(UtenteQueryGetById request, CancellationToken ct)
    {
        var idEnte = request.AuthenticationInfo!.IdEnte;
        var idUtente = request.AuthenticationInfo!.Id;
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new UtenteQueryGetByIdPersistence(idEnte!, idUtente!), ct);
    }
} 