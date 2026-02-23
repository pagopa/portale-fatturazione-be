using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.QueryHandlers;

public class ApiKeyQueryGetByKeyEnteHandler(
    ISelfCareDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    IAesEncryption encryption,
    ILogger<ApiKeyQueryGetByKeyEnteHandler> logger) : IRequestHandler<ApiKeyQueryGetByKeyEnte, ApiKeyEnteDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<ApiKeyQueryGetByKeyEnteHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IAesEncryption _encryption = encryption;
    public async Task<ApiKeyEnteDto?> Handle(ApiKeyQueryGetByKeyEnte request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new ApiKeyQueryGetByKeyEntePersistence(request, _encryption), ct);
    }
}