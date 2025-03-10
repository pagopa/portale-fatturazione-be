using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
public class FattureQueryInvioSapMultiploPeriodoHandler(
 ISelfCareDbContextFactory factory, 
 IStringLocalizer<Localization> localizer,
 IMediator handler,
 ILogger<FattureQueryInvioSapMultiploPeriodoHandler> logger) : IRequestHandler<FattureInvioSapMultiploPeriodoQuery, IEnumerable<FatturaInvioMultiploSapPeriodo>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureQueryInvioSapMultiploPeriodoHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    private readonly IMediator _handler = handler;
    public async Task<IEnumerable<FatturaInvioMultiploSapPeriodo>?> Handle(FattureInvioSapMultiploPeriodoQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureQueryInvioSapMultiploPeriodoPersistence(request), ct); 
    }
}