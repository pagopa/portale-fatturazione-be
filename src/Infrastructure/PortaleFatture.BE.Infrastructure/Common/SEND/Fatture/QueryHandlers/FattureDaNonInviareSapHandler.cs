using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class FattureDaNonInviareSapHandler(
         IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<FattureDaNonInviareSapHandler> logger) : IRequestHandler<FattureDaNonInviareSapQuery, FattureDaNonInviareSapDto?>
    {

    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<FattureDaNonInviareSapHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<FattureDaNonInviareSapDto?> Handle(FattureDaNonInviareSapQuery request, CancellationToken ct)
    {
        using var rs = await _factory.Create(cancellationToken: ct);
        return await rs.Query(new FattureDaNonInviareSapPersistence(request), ct);
    }

}

