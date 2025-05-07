using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.QueryHandlers;

public class ApiKeyQueryGetByKeyHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer, 
    IAesEncryption encryption,
    ILogger<ApiKeyQueryGetByKeyHandler> logger) : IRequestHandler<ApiKeyQueryGetByKey, ApiKeyDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ApiKeyQueryGetByKeyHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IAesEncryption _encryption = encryption;
    public async Task<ApiKeyDto?> Handle(ApiKeyQueryGetByKey request, CancellationToken ct)
    {
        using var rs = await _factory.Create(true, cancellationToken: ct);
        {
            var key = await rs.Query(new ApiKeyQueryGetByKeyPersistence(request, _encryption), ct);
            if (!(key == null))
            {
                var check = await rs.Query(new CheckApiKeyQueryGetPersistence(new ApiKeyQueryGet(new AuthenticationInfo()
                {
                    IdEnte = key.IdEnte
                })
                { }, _encryption), ct);
                if (!check.IsNullNotAny())
                {
                    key!.ApiKey = key.ApiKey == null ? null : _encryption.DecryptString(key.ApiKey);
                    return key;
                } 
                else
                    return null;
            }
            return key;
        }
    }
}