using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Storici;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands;
using PortaleFatture.BE.Infrastructure.Common.Storici.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.CommandHandlers;

public class FattureDaNonInviareSapCancellazioneCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureDaNonInviareSapCancellazioneCommandHandler> logger) : IRequestHandler<FattureDaNonInviareSapCancellazioneCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly IMediator _handler = handler;
    private readonly ILogger<FattureDaNonInviareSapCancellazioneCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<int?> Handle(FattureDaNonInviareSapCancellazioneCommand command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        var result = await uow.Execute(new FattureDaNonInviareSapCancellazioneCommandPersistence(command, _localizer), ct);
        return result - command.Fatture!.Count();
    }

}