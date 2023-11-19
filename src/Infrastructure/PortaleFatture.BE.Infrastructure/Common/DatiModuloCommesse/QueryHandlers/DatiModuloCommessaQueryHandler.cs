using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryHandler : IRequestHandler<DatiModuloCommessaQueryGet, ModuloCommessaDto?>
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

        public async Task<ModuloCommessaDto?> Handle(DatiModuloCommessaQueryGet request, CancellationToken ct)
        {
            var adesso = DateTime.UtcNow.ItalianTime();
            var (anno, mese) = adesso.YearMonth();
            request.AnnoValidita = anno;
            request.MeseValidita = mese;
            using var uow = await _factory.Create(true, cancellationToken: ct); 
            var datic = await uow.Query(new DatiModuloCommessaQueryGetByIdPersistence(request.IdEnte, anno, mese, request.IdTipoContratto, request.Prodotto), ct);
            var datit = await uow.Query(new DatiModuloCommessaTotaleQueryGetByIdPersistence(request.IdEnte, anno, mese, request.IdTipoContratto, request.Prodotto), ct);
            return new ModuloCommessaDto()
            {
                DatiModuloCommessa = datic!,
                DatiModuloCommessaTotale = datit!
            };
        }
    }
}