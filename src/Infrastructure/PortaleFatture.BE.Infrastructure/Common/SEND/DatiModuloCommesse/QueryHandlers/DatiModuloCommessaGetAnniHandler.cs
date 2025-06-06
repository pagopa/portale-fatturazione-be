﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaGetAnniHandler : IRequestHandler<DatiModuloCommessaGetAnni, IEnumerable<string>?>
    {
        private readonly IFattureDbContextFactory _factory;
        private readonly ILogger<DatiModuloCommessaGetAnniHandler> _logger;
        private readonly IStringLocalizer<Localization> _localizer;

        public DatiModuloCommessaGetAnniHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiModuloCommessaGetAnniHandler> logger)
        {
            _factory = factory;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IEnumerable<string>?> Handle(DatiModuloCommessaGetAnni request, CancellationToken ct)
        {
            var idEnte = request.AuthenticationInfo.IdEnte;
            var prodotto = request.AuthenticationInfo.Prodotto;
            using var uow = await _factory.Create(cancellationToken: ct);
            return await uow.Query(new DatiModuloCommessaQueryGetAnniPersistence(idEnte, prodotto), ct);
        }
    }
}