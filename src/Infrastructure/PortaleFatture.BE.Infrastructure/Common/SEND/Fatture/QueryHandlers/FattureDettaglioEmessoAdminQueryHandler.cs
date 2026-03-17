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


    public class FattureDettaglioEmessoAdminQueryHandler(IFattureDbContextFactory factory,
        IStringLocalizer<Localization> localizer,
        ILogger<FattureDettaglioEmessoAdminQueryHandler> logger) : IRequestHandler<FattureDocContabileDettaglioAdminEmessoQuery, IEnumerable<FatturaDocContabileEmessoDettaglioDto>>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<FattureDettaglioEmessoAdminQueryHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        public async Task<IEnumerable<FatturaDocContabileEmessoDettaglioDto>> Handle(FattureDocContabileDettaglioAdminEmessoQuery request, CancellationToken ct)
        {
            using var rs = await _factory.Create(cancellationToken: ct);
            var rawResult = await rs.Query(new FattureDettaglioEmessoAdminPersistence(request), ct);

            return rawResult;
        }
    }