using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.CommandHandlers;

public class ReportNotificheUpdateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportNotificheUpdateCommandHandler> logger) : IRequestHandler<ReportNotificheUpdateCommand, string?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ReportNotificheUpdateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<string?> Handle(ReportNotificheUpdateCommand command, CancellationToken ct)
    { 
        using var uow = await _factory.Create(false, cancellationToken: ct);
        var id = await uow.Execute(new ReportNotificheUpdateCommandPersistence(command, _localizer), ct);
        if (id > 0) 
            return command.UniqueId; 
        else 
            uow.Rollback();
            return string.Empty; 
    }
}