using Azure.Core;
using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

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
            var (anno, mese, _) = Time.YearMonth();
            var idTipoContratto = request.AuthenticationInfo.IdTipoContratto; 
            var prodotto = request.AuthenticationInfo.Prodotto;

            using (var rs = await _factory.Create(true, cancellationToken: ct))
            {
                var prodotti = await rs.Query(new ProdottoQueryGetAllPersistence(), ct);
                if (prodotti.IsNullNotAny())
                {
                    var msg = "Provide products in configurazion!";
                    _logger.LogError(msg);
                    throw new ConfigurationException(msg);
                } 
                prodotto = prodotti.Where(x => x.Nome!.ToLower() == prodotto!.ToLower()).Select(x=> x.Nome).FirstOrDefault();
                if (prodotto == null)
                {
                    var msg = "I could not find the specified product!";
                    _logger.LogError(msg);
                    throw new ConfigurationException(msg);
                }
                var contratti = await rs.Query(new TipoContrattoQueryGetAllPersistence(), ct);
                if (contratti.IsNullNotAny())
                {
                    var msg = "Provide contracts in configurazion!";
                    _logger.LogError(msg);
                    throw new ConfigurationException(msg);
                }
                idTipoContratto = contratti.Where(x => x.Id! == idTipoContratto!).Select(x => x.Id).FirstOrDefault();
                if (idTipoContratto == null)
                {
                    var msg = "I could not find the specified contruct!";
                    _logger.LogError(msg);
                    throw new ConfigurationException(msg);
                }
            }

            request.AnnoValidita = request.AnnoValidita != null ? request.AnnoValidita : anno;
            request.MeseValidita = request.MeseValidita != null ? request.MeseValidita : mese;
            request.Prodotto = prodotto;
            request.IdTipoContratto = idTipoContratto.Value;

            var idEnte = request.AuthenticationInfo.IdEnte;

            using var uow = await _factory.Create(true, cancellationToken: ct); 
            var datic = await uow.Query(new DatiModuloCommessaQueryGetByIdPersistence(idEnte, request.AnnoValidita.Value, request.MeseValidita.Value, request.IdTipoContratto, request.Prodotto), ct);
            var datit = await uow.Query(new DatiModuloCommessaTotaleQueryGetByIdPersistence(idEnte, request.AnnoValidita.Value, request.MeseValidita.Value, request.IdTipoContratto, request.Prodotto), ct);

            if (datic!.IsNullNotAny() || datit!.IsNullNotAny())
                return null;

            return new ModuloCommessaDto()
            {
                DatiModuloCommessa = datic!,
                DatiModuloCommessaTotale = datit!
            };
        }
    }
}