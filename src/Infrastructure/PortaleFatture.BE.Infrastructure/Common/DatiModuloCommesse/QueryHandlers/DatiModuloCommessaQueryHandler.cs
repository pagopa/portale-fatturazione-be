using System.Reflection.Metadata;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryHandler : IRequestHandler<DatiModuloCommessaQueryGet, IEnumerable<DatiModuloCommessa>?>
    {
        private readonly IFattureDbContextFactory _factory;
        private readonly ILogger<DatiModuloCommessaQueryHandler> _logger;
        private readonly IStringLocalizer<Localization> _localizer;

        public DatiModuloCommessaQueryHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiModuloCommessaQueryHandler> logger)
        {
            _factory = factory;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IEnumerable<DatiModuloCommessa>?> Handle(DatiModuloCommessaQueryGet request, CancellationToken ct)
        {
            var adesso = DateTime.UtcNow.ItalianTime();
            var (anno, mese) = adesso.YearMonth();
            request.AnnoValidita = anno;
            request.MeseValidita = mese;
            using var uow = await _factory.Create(cancellationToken: ct);
            return await uow.Query(new DatiModuloCommessaQueryGetByIdPersistence(
                idEnte: request.IdEnte,
                annoValidita: request.AnnoValidita,
                meseValidita: request.MeseValidita,
                idTipoContratto: null,
                prodotto: null), ct);
        }
    }
}