using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Extensions;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Report.CommandHandlers;

public class ReportCreateCommandHandler(
 ISelfCareDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ReportCreateCommandHandler> logger) : IRequestHandler<ReportCreateCommand, ReportDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ReportCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<ReportDto?> Handle(ReportCreateCommand command, CancellationToken ct)
    {
        ReportDto report;
        using var dbContext = await _factory.Create(cancellationToken: ct);
        var idReport = await dbContext.Execute(new ReportCreateCommandPersistence(command, _localizer), ct);
        if (idReport > 0)
            return report = command.Map(idReport);

        throw new Exception("Report non valido.");
    }
}