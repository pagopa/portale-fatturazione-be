using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.QueryHandlers;

public class RelRigheSospeseQueryGetByIdHandler(
 ISelfCareDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<RelRigheSospeseQueryGetByIdHandler> logger) : IRequestHandler<RelRigheSospeseQueryGetById, IEnumerable<RigheRelDto>?>
{
    private readonly ISelfCareDbContextFactory _factory = factory;
    private readonly ILogger<RelRigheSospeseQueryGetByIdHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<IEnumerable<RigheRelDto>?> Handle(RelRigheSospeseQueryGetById request, CancellationToken ct)
    {

        using var rs = await _factory.Create(true, cancellationToken: ct);
        var dati = RelTestataKey.Deserialize(request.IdTestata!);
        var testata = await rs.Query(new RelTestataSospesaQueryGetByIdEntePersistence(new RelTestataSospesaQueryGetByIdEnte(request.AuthenticationInfo)
        {
            Anno = dati.Anno,
            Mese = dati.Mese,
            TipologiaFattura = dati.TipologiaFattura,
        }), ct);
        request.FlagConguaglio = testata!.RelTestate!.FirstOrDefault()!.FlagConguaglio;
        return await rs.Query(new RelRigheSospeseQueryGetByIdPersistence(request), ct);
    }
}