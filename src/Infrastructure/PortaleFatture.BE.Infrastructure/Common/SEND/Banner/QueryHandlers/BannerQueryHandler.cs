using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.QueryHandlers;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Banner.QueryHandlers;

public class BannerQueryHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<BannerQueryHandler> logger) : IRequestHandler<BannerQuery, BannerDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<BannerQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<BannerDto?> Handle(BannerQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        try
        {
            return await rs.Query(new BannerQueryPersistence(), ct);
        }
        catch
        {
            return null;
        }
    }
}