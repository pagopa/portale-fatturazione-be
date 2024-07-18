using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.CommandHandlers;

public class RelFatturabileByIdEntiCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<RelFatturabileByIdEntiCommandHandler> logger) : IRequestHandler<RelFatturabileByIdEnti, int?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<RelFatturabileByIdEntiCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int?> Handle(RelFatturabileByIdEnti command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        { 
            var rowAffected = await uow.Execute(new RelFatturabileByIdEntiCommandPersistence(command, _localizer), ct);
            return (rowAffected);
        }
        throw new DomainException(_localizer["xxx"]);
    }
}