using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers;

public class FattureEliminateQueryHandler(
    IFattureDbContextFactory factory,
    IStringLocalizer<Localization> localizer,
    ILogger<FattureEliminateQueryHandler> logger)
    : IRequestHandler<FattureEliminateQuery, FattureDocContabiliEliminateDtoList>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureEliminateQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<FattureDocContabiliEliminateDtoList> Handle(FattureEliminateQuery request, CancellationToken cancellationToken)
    {
        using var rs = await _factory.Create(cancellationToken: cancellationToken);
        var rawResult = await rs.Query(new FattureEliminateQueryPersistence(request), cancellationToken);

        return rawResult.ToEliminateDto();
    }
}

