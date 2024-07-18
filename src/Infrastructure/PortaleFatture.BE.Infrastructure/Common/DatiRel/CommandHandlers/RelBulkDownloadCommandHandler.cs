using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class RelBulkDownloadCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<RelBulkDownloadCommandHandler> logger) : IRequestHandler<RelBulkDownloadCommand, bool?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<RelBulkDownloadCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<bool?> Handle(RelBulkDownloadCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        { 
            var rowAffected = await uow.Execute(new RelBulkUploadCreateCommandPersistence(command, _localizer), ct);
            return (rowAffected == command.Commands!.Count);
        }
        throw new DomainException(_localizer["xxx"]);
    }
}