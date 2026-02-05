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

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.QueryHandlers
{
    public class FattureCreditoSospesoQueryHandler(IFattureDbContextFactory factory,
        IStringLocalizer<Localization> localizer,
        ILogger<FattureCreditoSospesoQueryHandler> logger) : IRequestHandler<FattureCreditoSospesoQuery, FattureCreditoSospesoDtoList>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<FattureCreditoSospesoQueryHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        public async Task<FattureCreditoSospesoDtoList> Handle(FattureCreditoSospesoQuery request, CancellationToken ct)
        {
            using var rs = await _factory.Create(cancellationToken: ct);
            var rawResult = await rs.Query(new FattureCreditoSospesoQueryPersistence(request), ct);

            return rawResult.ToGroupedDto();
        }
    }
}
