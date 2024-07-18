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

public class RelRigheQueryGetByIdHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelRigheQueryGetByIdHandler> logger) : IRequestHandler<RelRigheQueryGetById, IEnumerable<RigheRelDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelRigheQueryGetByIdHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<RigheRelDto>?> Handle(RelRigheQueryGetById request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new RelRigheQueryGetByIdPersistence(request), ct);
    }
}