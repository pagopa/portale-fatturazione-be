using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiRel.QueryHandlers;

public class RelVerificaByIdEntiQueryHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelVerificaByIdEntiQueryHandler> logger) : IRequestHandler<RelVerificaByIdEnti, RelVerificaDto?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelVerificaByIdEntiQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<RelVerificaDto?> Handle(RelVerificaByIdEnti request, CancellationToken ct)
    {
        var verifica = new RelVerificaDto();
        //using var rs = await _factory.Create(cancellationToken: ct);
        //return await rs.Query(new RelUploadQueryGetByIdPersistence(request), ct);
        return verifica;
    }
}