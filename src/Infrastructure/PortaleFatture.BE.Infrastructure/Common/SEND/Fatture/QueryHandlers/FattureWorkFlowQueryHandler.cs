﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;
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