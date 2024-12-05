using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.QueryHandlers;

public class EnteCodiceSDIHandler : IRequestHandler<EnteQueryCodiceSDIGetById, EnteContrattoDto?>
{
    private readonly ISelfCareDbContextFactory _factory;
    private readonly ILogger<EnteCodiceSDIHandler> _logger;
    private readonly IStringLocalizer<Localization> _localizer;

    public EnteCodiceSDIHandler(
     ISelfCareDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     ILogger<EnteCodiceSDIHandler> logger)
    {
        _factory = factory;
        _localizer = localizer;
        _logger = logger;
    }
    public async Task<EnteContrattoDto?> Handle(EnteQueryCodiceSDIGetById request, CancellationToken ct)
    {
        var idEnte = request.AuthenticationInfo!.IdEnte;
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(idEnte!), ct);
    }
}