using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Asseverazione.QueryHandlers;

public class AsseverazioneQueryGetHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IMediator handler,
    ILogger<AsseverazioneQueryGetHandler> logger) : IRequestHandler<AsseverazioneQueryGet, IEnumerable<EnteAsserverazioneDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<AsseverazioneQueryGetHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;
    public async Task<IEnumerable<EnteAsserverazioneDto>?> Handle(AsseverazioneQueryGet request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new AsseverazioneQueryGetPersistence(request), ct);
    }
}