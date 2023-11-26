using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryByAnnoHandler : IRequestHandler<DatiModuloCommessaQueryGetByAnno, IEnumerable<ModuloCommessaByAnnoDto>?>
    {
        private readonly IFattureDbContextFactory _factory;
        private readonly ILogger<DatiModuloCommessaQueryByAnnoHandler> _logger;
        private readonly IStringLocalizer<Localization> _localizer;

        public DatiModuloCommessaQueryByAnnoHandler(
         IFattureDbContextFactory factory,
         IStringLocalizer<Localization> localizer,
         ILogger<DatiModuloCommessaQueryByAnnoHandler> logger)
        {
            _factory = factory;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IEnumerable<ModuloCommessaByAnnoDto>?> Handle(DatiModuloCommessaQueryGetByAnno request, CancellationToken ct)
        {
            var (anno, _, _) = Time.YearMonth();
            request.AnnoValidita = request.AnnoValidita != null ? request.AnnoValidita : anno;
            var idEnte = request.AuthenticationInfo.IdEnte;
            IEnumerable<DatiModuloCommessaTotale>? dati;
            IEnumerable<CategoriaSpedizione>? categorie;
            using (var uow = await _factory.Create(true, cancellationToken: ct))
            {
                dati = await uow.Query(new DatiModuloCommessaQueryGetByAnnoPersistence(idEnte, request.AnnoValidita.Value), ct);
                categorie = await uow.Query(new SpedizioneQueryGetAllPersistence(), ct);
            }

            if (categorie!.IsNullNotAny())
            {
                var msg = "Provide categorie and tipo commessa in configuration!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }

            if (dati!.IsNullNotAny())
                return null;

            var dicCategorie = categorie!
                 .Select(x => new KeyValuePair<int, string>(x.Id, x.Tipo!))
                 .ToDictionary(x => x.Key, x => x.Value); 

            Dictionary<int, ModuloCommessaByAnnoDto> lista = new();
            foreach (var dato in dati!)
            {
                lista.TryGetValue(dato.MeseValidita, out var modulo);
                modulo ??= new()
                    {
                        Prodotto = dato.Prodotto,
                        AnnoValidita = dato.AnnoValidita,
                        IdEnte = dato.IdEnte,
                        IdTipoContratto = dato.IdTipoContratto, 
                        MeseValidita = dato.MeseValidita,
                        Stato = dato.Stato,
                        Totali = new()
                    };  

                modulo.Totali!.TryGetValue(dato.IdCategoriaSpedizione, out var totMese);
                totMese = new()
                {
                    IdCategoriaSpedizione = dato.IdCategoriaSpedizione,
                    Tipo = dicCategorie[dato.IdCategoriaSpedizione],
                    TotaleCategoria = dato.TotaleCategoria
                }; 
                modulo.Totali[dato.IdCategoriaSpedizione] = totMese;
                modulo.Totale  = modulo.Totali.Sum(x => x.Value.TotaleCategoria);
                modulo.Modifica = dato.Stato == StatoModuloCommessa.ApertaCaricato && request.AuthenticationInfo.Ruolo == Ruolo.ADMIN;
                lista[dato.MeseValidita] = modulo;
            }

            return lista.Select(x => x.Value);
        }
    }
}