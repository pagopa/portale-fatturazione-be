using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.QueryHandlers;

public class RelUploadQueryGetByIdHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelUploadQueryGetByIdHandler> logger) : IRequestHandler<RelUploadGetById, IEnumerable<RelUpload>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelUploadQueryGetByIdHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<RelUpload>?> Handle(RelUploadGetById request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelUploadQueryGetByIdPersistence(request), ct);
    }
}