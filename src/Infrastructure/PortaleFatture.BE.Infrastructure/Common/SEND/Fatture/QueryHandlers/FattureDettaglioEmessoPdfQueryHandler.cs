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

    /// <summary>
    /// Handles queries for retrieving detailed information about invoices in PDF format.
    /// </summary>
    /// <remarks>This handler is typically used within a MediatR pipeline to process requests for detailed
    /// invoice data, returning results as data transfer objects. The handler is stateless and can be reused
    /// across multiple requests.</remarks>
    /// <param name="factory">The factory used to create instances of the invoices database context. Cannot be null.</param>
    /// <param name="localizer">The string localizer used for localization of messages and resources. Cannot be null.</param>
    /// <param name="logger">The logger used to record diagnostic and operational information. Cannot be null.</param>

    public class FattureDettaglioEmessoPdfQueryHandler(IFattureDbContextFactory factory,
        IStringLocalizer<Localization> localizer,
        ILogger<FattureDettaglioEmessoPdfQueryHandler> logger) : IRequestHandler<FattureDettaglioEmessoPdfQuery, IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<FattureDettaglioEmessoPdfQueryHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        public async Task<IEnumerable<FatturaDocContabileEmessoDettaglioDto>> Handle(FattureDettaglioEmessoPdfQuery request, CancellationToken ct)
        {
            using var rs = await _factory.Create(cancellationToken: ct);
            var rawResult = await rs.Query(new FattureDettaglioEmessoPdfPersistence(request), ct);
            return rawResult;
        }
    }