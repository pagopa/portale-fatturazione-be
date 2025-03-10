using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
public class FattureQueryInvioSapMultiploHandler(
 ISelfCareDbContextFactory factory, 
 IStringLocalizer<Localization> localizer,
 IMediator handler,
 ILogger<FattureQueryInvioSapMultiploHandler> logger) : IRequestHandler<FattureInvioSapMultiploQuery, IEnumerable<FatturaInvioMultiploSap>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureQueryInvioSapMultiploHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    private readonly IMediator _handler = handler;
    public async Task<IEnumerable<FatturaInvioMultiploSap>?> Handle(FattureInvioSapMultiploQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureQueryInvioSapMultiploPersistence(request), ct); 
    }
}