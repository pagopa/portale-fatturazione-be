using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.QueryHandlers;

public class CalendarioContestazioneAllQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<CalendarioContestazioneQueryHandler> logger) : IRequestHandler<CalendarioContestazioneQueryGetAll, IEnumerable<CalendarioContestazione>?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<CalendarioContestazioneQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<CalendarioContestazione>?> Handle(CalendarioContestazioneQueryGetAll request, CancellationToken ct)
    {

        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new CalendarioContestazioneQueryGetAllPersistence(request), ct);
    }
}