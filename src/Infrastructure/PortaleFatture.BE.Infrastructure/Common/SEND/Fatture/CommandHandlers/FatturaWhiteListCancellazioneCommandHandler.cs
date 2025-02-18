using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

internal class FatturaWhiteListCancellazioneCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FatturaWhiteListCancellazioneCommandHandler> logger) : IRequestHandler<FatturaWhiteListCancellazioneCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FatturaWhiteListCancellazioneCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int?> Handle(FatturaWhiteListCancellazioneCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create( cancellationToken: ct);
        var result = await uow.Execute(new FatturaWhiteListCancellazioneCommandPersistence(command, _localizer), ct);
        return result - command.Ids!.Count();
    }
}