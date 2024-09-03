using System.Collections.Generic;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Fatture;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.Fatture.QueryHandlers;
public class FattureWorkFlowQueryHandler(
 IMediator handler,
 ISelfCareDbContextFactory factory,
 IServiceWorkFlowFatture workFlowService,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureWorkFlowQueryHandler> logger) : IRequestHandler<FattureWorkFlowQuery, IEnumerable<FattureVerifyModifica>?>
{
    private readonly IMediator _handler = handler;
    private readonly IServiceWorkFlowFatture _workFlowService = workFlowService;
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<FattureWorkFlowQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<FattureVerifyModifica>?> Handle(FattureWorkFlowQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(true, cancellationToken: ct);
        return await rs.Query(new FattureWorkFlowQueryPersistence(request), ct); 
    }
}