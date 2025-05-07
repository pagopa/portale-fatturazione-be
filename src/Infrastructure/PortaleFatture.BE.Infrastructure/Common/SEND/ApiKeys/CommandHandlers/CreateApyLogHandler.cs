using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.CommandHandlers;

public class CreateApyLogHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<CreateApyLogHandler> logger) : IRequestHandler<CreateApyLogCommand, int?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<CreateApyLogHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<int?> Handle(CreateApyLogCommand request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        { 
            return await rs.Execute(new CreateApyLogPersistence(request), ct);
        }
    }
}
