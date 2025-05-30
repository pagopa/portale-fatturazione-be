﻿using System.Security;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.QueryHandlers;

public class ApiKeyIpsQueryGetHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer, 
    IAesEncryption encryption,
    ILogger<ApiKeyIpsQueryGetHandler> logger) : IRequestHandler<ApiKeyIpsQueryGet, IEnumerable<ApiKeyIpsDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ApiKeyIpsQueryGetHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer; 
    private readonly IAesEncryption _encryption = encryption;
    public async Task<IEnumerable<ApiKeyIpsDto>?> Handle(ApiKeyIpsQueryGet request, CancellationToken ct)
    {
        using var rs = await _factory.Create(true, cancellationToken: ct);
        {
            var result = await rs.Query(new CheckApiKeyQueryGetPersistence(new ApiKeyQueryGet(request.AuthenticationInfo)
            {
                IdEnte = request.IdEnte
            }, _encryption), ct); 
            if(result.IsNullNotAny())
                throw new SecurityException("Ente non registrato!");
            return await rs.Query(new ApiKeyIpsQueryGetPersistence(request, _encryption), ct);
        } 
    }
}