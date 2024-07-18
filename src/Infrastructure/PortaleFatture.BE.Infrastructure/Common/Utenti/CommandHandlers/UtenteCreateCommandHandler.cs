using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Utenti;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Commands;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Utenti.CommandHandlers;

public class UtenteCreateCommandHandler : IRequestHandler<UtenteCreateCommand, Utente?>
{
    private readonly IFattureDbContextFactory _factory;
    private readonly ILogger<UtenteCreateCommandHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public UtenteCreateCommandHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<UtenteCreateCommandHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }
    public async Task<Utente?> Handle(UtenteCreateCommand command, CancellationToken ct)
    {
        var (_, _, _, adesso) = Time.YearMonthDay();
        command.DataUltimo = command.DataUltimo == null ? adesso : command.DataUltimo.Value;
        command.DataPrimo = command.DataPrimo == null ? adesso : command.DataPrimo.Value;
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Execute(new UtenteCreateCommandPersistence(command), ct);
    }
}