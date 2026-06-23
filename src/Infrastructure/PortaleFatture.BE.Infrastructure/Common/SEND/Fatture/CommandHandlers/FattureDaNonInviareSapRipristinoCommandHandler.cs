using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FattureDaNonInviareSapRipristinoCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureDaNonInviareSapRipristinoCommandHandler> logger) : IRequestHandler<FattureDaNonInviareSapRipristinoCommand, int?>
{

    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FattureDaNonInviareSapRipristinoCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int?> Handle(FattureDaNonInviareSapRipristinoCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var result = await uow.Execute(new FattureDaNonInviareSapRipristinoCommandPersistence(command, _localizer), ct);
        return result - command.Fatture!.Count();
    }

}