using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class GestioneFattureAzioneCommandHandler(
    IFattureDbContextFactory factory,
    IMediator handler,
    IStringLocalizer<Localization> localizer,
    ILogger<GestioneFattureAzioneCommandHandler> logger): IRequestHandler<GestioneFattureAzioneCommand, bool?>  
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<GestioneFattureAzioneCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(GestioneFattureAzioneCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        // niente try/catch: se fallisce, l'eccezione risale al controller
        var result = await uow.Execute(new GestioneFattureAzioneCommandPersistence(command, _localizer), ct);
        return result;
    }
}


