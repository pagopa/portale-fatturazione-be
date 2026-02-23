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
    /// <summary>
    /// Gestore della query per il recupero delle fatture emesse
    /// </summary>
    /// <param name="factory"></param>
    /// <param name="localizer"></param>
    /// <param name="logger"></param>
    public class FattureEmesseQueryHandler(IFattureDbContextFactory factory,
        IStringLocalizer<Localization> localizer,
        ILogger<FattureEmesseQueryHandler> logger) : IRequestHandler<FattureEmesseQuery, FattureDocContabiliDtoList>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<FattureEmesseQueryHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;

        /// <summary>
        /// Gestisce la query per il recupero delle fatture emesse
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<FattureDocContabiliDtoList> Handle(FattureEmesseQuery request, CancellationToken ct)
        {
            using var rs = await _factory.Create(cancellationToken: ct);
            var rawResult = await rs.Query(new FattureEmesseQueryPersistence(request), ct);

            return rawResult.ToGroupedDto();
        }
    }
}
