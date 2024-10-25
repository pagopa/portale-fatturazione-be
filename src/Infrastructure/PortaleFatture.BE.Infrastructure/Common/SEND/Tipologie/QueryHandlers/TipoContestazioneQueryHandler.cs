using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.QueryHandlers;

public class TipoContestazioneQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<TipoContestazioneQueryHandler> logger) : IRequestHandler<TipoContestazioneGetAll, IEnumerable<TipoContestazione>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<TipoContestazioneQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<TipoContestazione>> Handle(TipoContestazioneGetAll request, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new TipoContestazioneQueryGetAllPersistence(), ct);
    }
}