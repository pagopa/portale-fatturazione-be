using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.AnagraficaPSP.QueryHandlers;

public sealed class PSPQueryGetByNameHandler(
  IFattureDbContextFactory factory,
  IStringLocalizer<Localization> localizer,
  ILogger<PSPQueryGetByNameHandler> logger) : IRequestHandler<PSPQueryGetByName, IEnumerable<ContractIdPSP>>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<PSPQueryGetByNameHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;

    public async Task<IEnumerable<ContractIdPSP>> Handle(PSPQueryGetByName command, CancellationToken ct)
    {
        using var uow = await _factory.Create(cancellationToken: ct);
        return await uow.Query(new PSPQueryGetByNamePersistence(command), ct);
    }
} 