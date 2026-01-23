using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.InvioPsp.QueryHandlers; 
 

public sealed class PspEmailQueryGetQueryHandler(
  IFattureDbContextFactory factory,
  IStringLocalizer<Localization> localizer,
  ILogger<PspEmailQueryGetQueryHandler> logger) : IRequestHandler<PspEmailQueryGet, IEnumerable<PspEmailDto>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<PspEmailQueryGetQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<PspEmailDto>> Handle(PspEmailQueryGet command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new PspEmailQueryGetPersistence(command), ct);
    }
} 