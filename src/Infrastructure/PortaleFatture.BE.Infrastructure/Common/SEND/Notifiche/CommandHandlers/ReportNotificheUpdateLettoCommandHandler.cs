using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.CommandHandlers;

internal class ReportNotificheUpdateLettoCommandHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<ReportNotificheUpdateLettoCommandHandler> logger) : IRequestHandler<ReportNotificheUpdateLettoCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheUpdateLettoCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int?> Handle(ReportNotificheUpdateLettoCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var id = await uow.Execute(new ReportNotificheUpdateLettoCommandPersistence(command, _localizer), ct);
        return id;
    }
}
