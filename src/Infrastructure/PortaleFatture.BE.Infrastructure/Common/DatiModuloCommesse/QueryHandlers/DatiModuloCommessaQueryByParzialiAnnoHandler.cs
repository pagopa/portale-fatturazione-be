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
    public class DatiModuloCommessaQueryByParzialiAnnoHandler : IRequestHandler<DatiModuloCommessaParzialiQueryGetByAnno, IEnumerable<DatiModuloCommessaParzialiTotale>?>
    {
        private readonly IFattureDbContextFactory _factory;
        private readonly ILogger<DatiModuloCommessaQueryByParzialiAnnoHandler> _logger;
        private readonly IStringLocalizer<Localization> _localizer;

        public DatiModuloCommessaQueryByParzialiAnnoHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiModuloCommessaQueryByParzialiAnnoHandler> logger)
        {
            _factory = factory;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IEnumerable<DatiModuloCommessaParzialiTotale>?> Handle(DatiModuloCommessaParzialiQueryGetByAnno request, CancellationToken ct)
        {
            var (anno, _, _) = Time.YearMonth();
            request.AnnoValidita = request.AnnoValidita != null ? request.AnnoValidita : anno;
            var idEnte = request.AuthenticationInfo.IdEnte;
            var idTipoContratto = request.AuthenticationInfo.IdTipoContratto;
            var prodotto = request.AuthenticationInfo.Prodotto;
            var ruolo = request.AuthenticationInfo.Ruolo;
            using var uow = await _factory.Create(true, cancellationToken: ct);
            return await uow.Query(new DatiModuloCommessaParzialiTotaleQueryGetByIdPersistence(idEnte, request.AnnoValidita.Value, idTipoContratto, prodotto, ruolo), ct);
        }
    }
}