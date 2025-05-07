using System.Security;
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

public class ApiKeyQueryGetHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IAesEncryption encryption,
    ILogger<ApiKeyQueryGetHandler> logger) : IRequestHandler<ApiKeyQueryGet, IEnumerable<ApiKeyDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ApiKeyQueryGetHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IAesEncryption _encryption = encryption;
    public async Task<IEnumerable<ApiKeyDto>?> Handle(ApiKeyQueryGet request, CancellationToken ct)
    {
        using var rs = await _factory.Create(true, cancellationToken: ct);
        {
            var result = await rs.Query(new CheckApiKeyQueryGetPersistence(request, _encryption), ct);
            if(result.IsNullNotAny())
                throw new SecurityException("Ente non registrato!");
            var keys = await rs.Query(new ApiKeyQueryGetPersistence(request), ct);
            if (keys == null)
                return null;
            foreach (var key in keys) 
                key!.ApiKey = key.ApiKey == null ? null : _encryption.DecryptString(key.ApiKey); 
            return keys!;
        } 
    }
}